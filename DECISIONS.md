# Design decisions

## What this slice does

A visitor chooses a make and a model year, then sees the vehicle types for that make and the models for that make and year. Choosing a type reloads the models through the type-specific NHTSA call. The browser never calls vPIC itself.

## Makes are paged in our API

`getallmakes` returns the whole catalog in one payload. The API downloads that list, caches it, and returns a page of 40 names. The UI can pass a `prefix` so a search for `hon` does not ship every make to the browser, and it asks for the next page when the list is scrolled.

Trade-off: the first request in a process still waits on the full NHTSA download. After that, paging and prefix checks are local. A database would make the first byte faster and would let us page without holding the list in memory. For a read-only catalog and a few-hour assignment, the in-memory page is enough.

## Cache

| Data | How long it stays |
| --- | --- |
| All makes | 12 hours |
| Types for a make, and models for a make/year/type | 1 hour |

The cache is `IMemoryCache` inside the API process. A restart, or a second instance, starts cold. That is acceptable on one small machine. The next step is a shared cache so a deploy does not make the next visitor wait on `getallmakes` again.

## Model years start at 1996

The year list in the UI and `VehicleCatalog` both begin at 1996 and run through the current calendar year. Requests for 1995 and earlier are rejected before NHTSA is called. That keeps the picker and the API on the same rule.

## Names from NHTSA are trimmed

vPIC sometimes returns a vehicle type with a trailing space, such as `"Truck "`. The client stores the trimmed name and sends that trimmed name when a type is selected. The models URL is built from that value. A test covers the trim, because an untrimmed name misses the type filter.

## One host in production

The production Angular build calls `/api` on the same origin. nginx serves the static files and proxies `/api` and `/health` to the API container. Local `ng serve` is the exception: it runs on port 4200 and the API allows that origin in CORS. The deployed site does not need a wide-open CORS policy.

Swagger stays on in Development only. Production exposes `/health` for the container check.

## Docker

Two images:

- The API image publishes the .NET project and runs `VehicleExplorer.API.dll` on port 8080.
- The web image builds the Angular app, then copies the browser output into nginx.

Compose publishes the web container on port 8080 locally. On the EC2 instance `WEB_PORT=80` matches the security group. The instance is a `t3.micro`, so user-data adds a 2 GB swap file and builds one image at a time. Amazon Linux's Docker package does not include Buildx, and Compose 2.40 refuses to build without it, so user-data also installs the Buildx plugin.

## Assumptions

- NHTSA vPIC needs no API key and is reachable from the host that runs the API.
- A make id and a model year are enough to show types and models. The type control is a filter on top of that, using the fourth vPIC endpoint.
- One process and one instance are enough for review traffic.
- The public GitHub repository is what the instance clones. A launch before that push builds an older tree.

## Trade-offs I would revisit

- In-memory cache and in-memory paging of makes.
- HTTP only. HTTPS needs a certificate and a name; the free-tier box is addressed by its public DNS.
- No rate limit in front of NHTSA. The cache is the only protection against repeat clicks.
- No shared error page beyond the UI retry states and the API problem responses.

## Next steps

- Put the makes list in a store that survives restarts.
- Terminate TLS in front of nginx.
- Limit how often one client can miss the cache and call NHTSA.
- Add an integration test that boots the API with a stubbed NHTSA handler.

## How AI was used

An assistant helped scaffold the API, the Angular explorer, the tests, and the container files. I kept the parts that match the NHTSA payloads and the tests, and I checked the running behavior.

One thing it got wrong: while Swagger was being added, the assistant treated a document that said `openapi: "3.0.4"` as invalid and rewrote that field to `3.0.1`. The document was valid. Swagger UI shipped with Swashbuckle 6.6.2 rejects 3.0.4, and after a package upgrade the browser kept the old UI scripts for a week, so the page still failed. The API now serves the built-in OpenAPI document at `/openapi/v1.json` and loads Swagger UI 10, which accepts that version. The document is not rewritten.
