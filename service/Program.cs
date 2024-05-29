using System.Net.Mime;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

var httpsCertificate = X509Certificate2.CreateFromPemFile("./ssl/tls.crt", "./ssl/tls.key");
// Windows workaround
httpsCertificate = new X509Certificate2(httpsCertificate.Export(X509ContentType.Pfx));

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseKestrel(options => {
	options.ListenAnyIP(8080, listenOptions => {
		listenOptions.UseHttps(httpsCertificate);
	});
});
var dynamicConverter = new SystemTextJson.DynamicConverter.Converter();
builder.Services.ConfigureHttpJsonOptions(options => {
	options.SerializerOptions.Converters.Add(dynamicConverter);
});
var jsonSerializerOptions = new JsonSerializerOptions();
jsonSerializerOptions.Converters.Add(dynamicConverter);
var app = builder.Build();
app.UseHttpsRedirection();

object[] BuildJsonPatches(ILogger<Program> logger, IEnumerable<KeyValuePair<string, string>> kvps, string derExtension) =>
	kvps.Select(kvp => {
		try {
			var rsa = RSA.Create();
			var pemBytes = Convert.FromBase64String(kvp.Value);
			var pem = Encoding.UTF8.GetString(pemBytes);
			rsa.ImportFromPem(pem);
			var derBytes = rsa.ExportPkcs8PrivateKey();
			var base64Der = Convert.ToBase64String(derBytes);
			var patch = new { op = "add", path = $"/data/{kvp.Key}.{derExtension}", value = base64Der };
			return patch;
		} catch (Exception e) {
			logger.LogDerringError(e);
			return null;
		}
	}).OfType<object>().ToArray();

app.MapPost("/der", ([FromServices] ILogger<Program> logger, [FromBody] dynamic input) => {
	var annotations = input.request.@object.metadata.annotations as IDictionary<string, object?>;
	var parameters = new Parameters(annotations);
	var derExtension = parameters.DerExtension;
	var data = input.request.@object.data as IDictionary<string, object?>;
	var typedData = data?.Where(kvp => kvp.Value is string).Select(kvp => KeyValuePair.Create(kvp.Key, (string)kvp.Value!)).ToDictionary();
	var matches = typedData?.Where(kvp => {
		if (parameters.ShouldConvert(kvp.Key)) {
			var targetName = $"{kvp.Key}.{derExtension}";
			if (typedData.ContainsKey(targetName))
				logger.LogNameClash(kvp.Key, targetName);
			else
				return true;
		}
		return false;
	}) ?? [];
	var patches = BuildJsonPatches(logger, matches, derExtension);
	var jsonPatches = JsonSerializer.Serialize(patches, jsonSerializerOptions);
	var base64JsonPatches = Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonPatches));
	return new {
		apiVersion = "admission.k8s.io/v1beta1",
		kind = "AdmissionReview",
		response = new {
			allowed = true,
			patchType = "JSONPatch",
			patch = base64JsonPatches,
		},
	};
})
.Accepts<dynamic>(MediaTypeNames.Application.Json)
.Produces(StatusCodes.Status200OK, null, MediaTypeNames.Application.Json);

app.Run();