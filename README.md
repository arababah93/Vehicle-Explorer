# Vehicle Explorer

Pick a make and a model year, then browse the vehicle types and models from the [NHTSA vPIC](https://vpic.nhtsa.dot.gov/api/) catalog.

The Angular app talks only to this API. The API calls NHTSA.

## Run it locally

You need the .NET 10 SDK and Node.js 22 or newer.

Start the API:

```powershell
dotnet run --project Vehicle-Explorer-Backend --launch-profile http
```

The API listens on `http://localhost:5080`. OpenAPI is at `http://localhost:5080/swagger` while the environment is Development.

In a second terminal, start the UI:

```powershell
cd client
npm ci
npm start
```

Open `http://localhost:4200`. The dev build calls `http://localhost:5080/api`.

Optional tests:

```powershell
dotnet test VehicleExplorer.Tests/VehicleExplorer.Tests.csproj
```

## Run it with Docker

From the repository root:

```powershell
docker compose up --build
```

Open `http://localhost:8080`. nginx serves the Angular build and proxies `/api` and `/health` to the API container.

Stop it with `docker compose down`.

## Host it on AWS Free Tier

The live site is one Amazon Linux 2023 `t3.micro` instance. Port 80 is the only port opened. The instance installs Docker, clones this public repository, and runs the same Compose stack with `WEB_PORT=80`.

Push this repository to GitHub before you launch. The instance clones `https://github.com/arababah93/Vehicle-Explorer.git`, so it builds whatever is on `main`.

Configure the AWS CLI on your machine with `aws configure`. Leave the keys there. Do not put them in this repository.

```powershell
.\deploy\launch-free-tier.ps1
```

The script prints `http://<public-dns>`. The first boot compiles both images on a 1 GB instance, with a 2 GB swap file, so allow about 15 minutes before the page responds.

Stop the instance when you are finished reviewing it. The script prints the `aws ec2 stop-instances` command.

Live site: not published from this machine. After you push and run the script, it prints `http://<public-dns>`. Put that address here before you submit.
