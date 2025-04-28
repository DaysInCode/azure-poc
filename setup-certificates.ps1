$certPath = "$env:USERPROFILE\.cosomos\cosmos-emulator.certificate.pem"

Write-Host "Downloading Cosmos DB Emulator certificate..."
$response = Invoke-WebRequest -Uri "https://localhost:8081/_explorer/emulator.pem" -OutFile $certPath

Write-Host "Installing certificate..."
Import-Certificate -FilePath $certPath -CertStoreLocation Cert:\LocalMachine\Root