# WinFlow MSIX / Microsoft Store

Esta carpeta contiene la preparación de WinFlow para distribución mediante MSIX y Microsoft Store.

## Enfoque

La versión empaquetada mantiene el comportamiento principal de WinFlow, pero delega al paquete MSIX la instalación, actualización, inicio con Windows y desinstalación.

Cuando WinFlow detecta que se está ejecutando dentro de un paquete MSIX:

- no ejecuta el instalador propio;
- no ejecuta el desinstalador propio;
- no crea accesos directos ni registro de inicio mediante `InstallManager`;
- inicia directamente la bandeja y los atajos globales.

La versión standalone usada en GitHub conserva el flujo actual.

## Requisitos en el PC de compilación

- Windows 10/11 x64.
- .NET SDK compatible con `net10.0-windows`.
- Windows 10/11 SDK, porque el script utiliza `MakeAppx.exe`.
- PowerShell.

## Identidad de Microsoft Store

Antes de generar el paquete final, reserva `WinFlow` en Partner Center y copia exactamente:

- **Package/Identity/Name**
- **Package/Identity/Publisher**

No inventes estos valores. Deben coincidir con la identidad asignada por Microsoft Store.

## Generar los assets

`GenerateAssets.ps1` crea los PNG utilizados por el manifest a partir de `assets/icons/WinFlow-Icon.ico`, incluyendo tamaños base, escalas 200/400 y target sizes principales.

```powershell
powershell -ExecutionPolicy Bypass -File .\packaging\GenerateAssets.ps1
```

Los archivos se generan en `packaging/Assets/` y están ignorados por Git.

## Generar WinFlow.msix

Desde la raíz del repositorio, ejecuta:

```powershell
powershell -ExecutionPolicy Bypass -File .\packaging\BuildMsix.ps1 `
  -IdentityName "VALOR_DE_PARTNER_CENTER" `
  -Publisher "VALOR_DE_PARTNER_CENTER"
```

Opcionalmente puedes indicar una versión MSIX de cuatro componentes:

```powershell
-Version "1.0.0.0"
```

El script:

1. genera los assets;
2. publica WinFlow self-contained para `win-x64`;
3. crea una carpeta temporal de staging;
4. genera `AppxManifest.xml` usando la identidad real;
5. localiza `MakeAppx.exe` dentro de Windows SDK;
6. produce `msix-output/WinFlow_<version>_x64.msix`.

`msix-staging/`, `msix-output/` y `packaging/Assets/` están ignorados por Git.

## Firma y pruebas

Para publicar mediante Microsoft Store no necesitas comprar un certificado de firma CA: Microsoft Store firma el MSIX después de la certificación.

Para probar localmente un paquete antes de Store, puedes firmarlo con un certificado de prueba confiado en tu equipo o utilizar un flujo de desarrollo compatible con paquetes sin firma en Windows 11.

Antes de enviar la primera versión:

1. instala y prueba el MSIX;
2. comprueba `Alt+C` y `Alt+V`;
3. comprueba el icono de bandeja;
4. reinicia sesión y verifica el startup task;
5. comprueba la desinstalación desde Configuración;
6. ejecuta Windows App Certification Kit;
7. sube el `.msix` a Partner Center.

## Archivos

- `Package.appxmanifest.template`: manifest base para Microsoft Store.
- `GenerateAssets.ps1`: genera los iconos requeridos por MSIX.
- `BuildMsix.ps1`: compila y empaqueta WinFlow.
