# Design decisions

The app lets a visitor pick a make and a model year, then see the vehicle types for that make and the models for that make and year. Choosing a type reloads the models through the type-specific NHTSA call. The browser never calls vPIC itself.

## Decisions

**The API owns the NHTSA calls.** Angular talks only to this API. That keeps caching, timeouts, and DTO shaping in one place, and it avoids a browser CORS dependency on vPIC.

**Clean Architecture.** The API host is thin. Domain holds the make, type, and model types. Application holds the catalog use cases and paging. Infrastructure holds the NHTSA client. The host only composes those projects through dependency injection. The first version was a single Web API project. I split it so each layer can be explained and changed on its own.

**Makes are paged.** `getallmakes` returns the whole catalog in one payload. The API downloads that list, caches it, and returns a page of 40 names. The dropdown sends `prefix`, `skip`, and `take`. A search for `hon` does not ship every make to the browser, and scrolling asks for the next page. The first version returned every make in one response, which was too large for the control.

**Years start at 1996.** The year list in the UI and `VehicleCatalog` both run from 1996 through the current calendar year. Requests for 1995 and earlier are rejected before NHTSA is called.

**NHTSA names are trimmed.** vPIC sometimes returns a vehicle type with a trailing space, such as `"Truck "`. The stored name is trimmed, and that trimmed name is what the models URL sends. A test covers the trim, because an untrimmed name misses the type filter.

**One origin in production.** The production Angular build calls `/api` on the same host. nginx serves the static files and proxies `/api` and `/health` to the API container. Local `ng serve` is the exception: it runs on port 4200, and the API allows only that origin in CORS. Swagger stays on in Development. Production exposes `/health` for the container check.

**Two images.** The API image publishes the .NET project and runs `VehicleExplorer.API.dll` on port 8080. The web image builds the Angular app and copies the browser output into nginx. Compose publishes the web container on port 8080 locally. On the EC2 instance `WEB_PORT=80` matches the security group. The instance is a `t3.micro`, so startup adds a 2 GB swap file and builds one image at a time. Amazon Linux's Docker package does not include Buildx, and Compose 2.40 refuses to build without it, so startup also installs the Buildx plugin.

## Assumptions

- NHTSA vPIC needs no API key and is reachable from the host that runs the API.
- A make id and a model year are enough to show types and models. The type control is a filter on top of that, using the fourth vPIC endpoint.
- One process and one instance are enough for review traffic.
- The instance clones the public GitHub repository, so it builds whatever is on `main` at launch.

## Trade-offs

- The cache is `IMemoryCache` inside the API process. Makes stay for 12 hours. Types and models stay for 1 hour. A restart, or a second instance, starts cold. The first request in a process still waits on the full NHTSA makes download. After that, paging and prefix checks are local. A database would make the first byte faster and would page without holding the list in memory. For a read-only catalog, the in-memory page is enough.
- HTTP only. HTTPS needs a certificate and a name. The free-tier box is addressed by its public DNS.
- No rate limit in front of NHTSA. The cache is the only protection against repeat clicks.
- No shared error page beyond the UI retry states and the API problem responses.

## Next steps

- Put the makes list in a store that survives restarts.
- Terminate TLS in front of nginx.
- Limit how often one client can miss the cache and call NHTSA.
- Add an integration test that boots the API with a stubbed NHTSA handler.

## How AI was used

I used AI for implementation options and for troubleshooting. I checked the suggestions against the NHTSA payloads and the running app, and I kept the design calls above.

One thing it got wrong: while Swagger was being added, a document that said `openapi: "3.0.4"` was treated as invalid and rewritten to `3.0.1`. The document was valid. Swagger UI shipped with Swashbuckle 6.6.2 rejects 3.0.4, and after a package upgrade the browser kept the old UI scripts, so the page still failed. I stopped rewriting the document. The API now serves the built-in OpenAPI document at `/openapi/v1.json` and loads Swagger UI 10, which accepts that version.
