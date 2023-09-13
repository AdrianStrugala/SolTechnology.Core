The docker-compose contains emulator for full env needed for development/testing. Run it using: [docker compose up].


For Service Bus Emulator certificate generation run below script in taleCode\tests\TaleCode.FunctionalTests:

$Password = "P4ssw0rd@2137"
$Cert = New-SelfSignedCertificate -CertStoreLocation Cert:\CurrentUser\My -DnsName "localhost" -FriendlyName "localhost self-signed" -Subject "CN=localhost" -NotAfter $([datetime]::now.AddYears(2)) -KeyExportPolicy Exportable
$Cert | Export-PfxCertificate -FilePath ./cert/localhost.pfx -Password (ConvertTo-SecureString -String $Password -Force -AsPlainText)
Import-PfxCertificate -FilePath ./cert/localhost.pfx -CertStoreLocation Cert:\CurrentUser\Root -Password (ConvertTo-SecureString -String $Password -Force -AsPlainText)