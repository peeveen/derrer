internal class Parameters {
	const string DerrerAnnotationPrefix = "com.peeveen.derrer/";
	const string NamesAnnotation = DerrerAnnotationPrefix + "names";
	const string ExtensionsAnnotation = DerrerAnnotationPrefix + "extensions";
	const string DerExtensionAnnotation = DerrerAnnotationPrefix + "addExtension";

	internal string[] Names { get; } = [];
	internal string[] Extensions { get; } = ["key"];
	internal string DerExtension { get; } = string.Empty;

	internal Parameters(IDictionary<string, object?>? annotations) {
		if (annotations != null) {
			if (annotations.TryGetValue(NamesAnnotation, out var names) && names is string namesString && !string.IsNullOrEmpty(namesString.Trim()))
				Names = namesString.Split(Path.PathSeparator, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries); ;
			if (annotations.TryGetValue(ExtensionsAnnotation, out var extensions) && extensions is string extensionsString && !string.IsNullOrEmpty(extensionsString.Trim()))
				Extensions = extensionsString.Split(Path.PathSeparator, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries); ;
			if (annotations.TryGetValue(DerExtensionAnnotation, out var derExtension) && derExtension is string derExtensionString && !string.IsNullOrEmpty(derExtensionString.Trim()))
				DerExtension = derExtensionString.Trim();
		}
	}

	internal bool ShouldConvert(string key) {
		if (Names.Contains(key))
			return true;
		string ext = Path.GetExtension(key).Trim('.');
		return !string.IsNullOrEmpty(ext) && Extensions.Contains(ext);
	}
}