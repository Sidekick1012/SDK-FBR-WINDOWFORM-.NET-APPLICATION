# escape=\
# Build stage: compile the WinForms .NET Framework project (requires Windows containers)
FROM mcr.microsoft.com/dotnet/framework/sdk:4.8-windowsservercore-ltsc2019 AS build
SHELL ["powershell", "-Command", "$ErrorActionPreference = 'Stop';"]
WORKDIR C:\src

# Copy everything and restore/build the project
COPY . .

# Restore NuGet packages for the project
# NOTE: update the path below if your .csproj location differs
RUN nuget restore "HCR-E-INVOICING-SYSTEM/HCR-E-INVOICING-SYSTEM.csproj"

# Build in Release configuration and output to C:\out
RUN msbuild "HCR-E-INVOICING-SYSTEM/HCR-E-INVOICING-SYSTEM.csproj" /p:Configuration=Release /p:OutputPath="C:\out"

# Runtime stage: copy compiled artifacts
FROM mcr.microsoft.com/dotnet/framework/runtime:4.8-windowsservercore-ltsc2019 AS runtime
WORKDIR C:\app
COPY --from=build C:\out\ .

# Note: This is a WinForms GUI application. Running the GUI inside a container is not supported
# on typical Docker setups. This image is primarily useful to produce reproducible builds and
# to generate the compiled EXE which you can deploy to a Windows host.
#
# To persist the SQLite database outside the container, mount a host directory as a volume:
#   docker run -v "C:\host\data:C:\app" invoiceapp
# The application stores `einvoice.db` in the application directory by default.

ENTRYPOINT ["HCR-E-INVOICING-SYSTEM.exe"]
