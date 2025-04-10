# Derrer

[Helm](https://helm.sh) has some very useful [functions for creating TLS/SSL certificates and keys](https://helm.sh/docs/chart_template_guide/function_list/#cryptographic-and-security-functions).

But everything it produces is PEM-encoded.

I hit a problem where I wanted a container running a JDBC service to use a Helm-generated private key, but it would only accept a DER-encoded key.

Rather than muck around with `initContainers` or new entrypoint scripts running `openssl` commands, I decided to create Derrer.

## What is it?

Derrer is a Helm chart that installs a `MutatingWebhookConfiguration` and a webservice for that webhook.

Once installed, any appropriately-annotated `Secret` that is added to the cluster will be passed to the webservice for mutation. If the webservice finds something that appears to be a PEM-encoded private key in the secret, it will add a DER-encoded version of that same key to the secret.

## How to use it

First of all, install the Helm chart into your cluster:

```bash
helm install derrer oci://registry-1.docker.io/peeveen/derrer --version=1.1.0
```

> Use `--set mutateOnUpdate=false` if you _don't_ want the mutation to occur on `UPDATE` events. By default, it will
> affect `CREATE` _and_ `UPDATE` events.

Then, add an appropriate annotation to your `Secret` and, when it is added to the cluster, Derrer should jump into action and perform any requested conversions.

| Annotation name                   | Description                                                                                                                                                                                                                              | Default value  |
| --------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------- |
| `com.peeveen.derrer/names`        | A `:`-separated list of names. Any items in the `Secret` that match any of these names will be considered as candidates for conversion.                                                                                                  | _Empty string_ |
| `com.peeveen.derrer/extensions`   | A `:`-separated list of file extensions. Any items in the `Secret` that look like filenames with any of these extensions will be considered as candidates for conversion.                                                                | `key`          |
| `com.peeveen.derrer`              | If this is set to any non-blank value, Derrer will trigger. If you supply any of the above annotations, you don't need to use this one. This is just a shorthand annotation for when you want Derrer to trigger with all-default values. | _Empty string_ |
| `com.peeveen.derrer/addExtension` | When an item is converted to DER, this extension is added. For example, if an item called `tls.key` is converted, and the value of this annotation is `der`, the `Secret` will have a new item called `tls.key.der` added to it.         | `der`          |

> NOTE: Derrer won't do anything if `addExtension` is the only annotation you add. You need to use at least one of the others.
