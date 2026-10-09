# Documentation only. Replace placeholders privately; never commit the resulting file.
$env:ASPNETCORE_ENVIRONMENT = 'Production'
$env:ASPNETCORE_URLS = 'https://<trusted-api-host>:<tls-port>'
$env:AllowedHosts = '<trusted-api-host>'
$env:Authentication__Jwt__Issuer = '<issuer>'
$env:Authentication__Jwt__Audience = '<audience>'
$env:Authentication__Jwt__SigningKey = '<independently-generated-random-secret-at-least-32-bytes>'
$env:Authentication__Jwt__RefreshTokenPepper = '<different-random-secret-at-least-32-bytes>'
$env:Activation__ApplicationOrigin = 'https://<trusted-spa-host>'
$env:ConnectionStrings__DefaultConnection = 'Data Source=<absolute-existing-private-local-db-path>;Mode=ReadWrite;Default Timeout=3'
$env:Storage__LocalFileSystem = 'true'
$env:Storage__PrivateFilesRoot = '<absolute-existing-private-directory>'
# Omit CorsOrigins for same-origin hosting. For separate origins:
$env:Deployment__CorsOrigins__0 = 'https://<trusted-spa-host>'
# Direct TLS; supply a privately stored certificate and secret password:
$env:ASPNETCORE_Kestrel__Certificates__Default__Path = '<private-certificate-path>'
$env:ASPNETCORE_Kestrel__Certificates__Default__Password = '<private-certificate-password>'
# Trusted edge mode instead: backend address/firewall must restrict direct access.
# $env:Deployment__ProxyEnabled = 'true'
# $env:Deployment__KnownProxies__0 = '<individual-trusted-proxy-IP>'
# No Seeding or Activation:Delivery values in non-Development.
