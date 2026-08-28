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
- Windows 10/11 SDK, porque los scripts utilizan `MakeAppx.exe` y `SignTool.exe`.
- PowerShell.

## Identidad de Microsoft Store

La identidad reservada para WinFlow en Partner Center es:

- **Package/Identity/Name:** `FEAR-Labs.WinFlow`
- **Package/Identity/Publisher:** `CN=EFE2FF1A-8478-41FD-A0B3-9DBE8DFE43B1`

Estos valores ya están integrados en el manifest y son los valores por defecto de `BuildMsix.ps1`.

## Generar los assets

`GenerateAssets.ps1` crea los PNG utilizados por el manifest a partir de `assets/icons/WinFlow-Icon.ico`, incluyendo tamaños base, escalas 200/400 y target sizes principales.

```powershell
powershell -ExecutionPolicy Bypass -File .\packaging\GenerateAssets.ps1
```

Los archivos se generan en `packaging/Assets/` y están ignorados por Git.

## Generar WinFlow.msix

Desde la raíz del repositorio, ejecuta:

```powershell
powershell -ExecutionPolicy Bypass -File .\packaging\BuildMsix.ps1
```

Opcionalmente puedes indicar una versión MSIX de cuatro componentes:

```powershell
powershell -ExecutionPolicy Bypass -File .\packaging\BuildMsix.ps1 -Version "1.0.0.0"
```

El script:

1. genera los assets;
2. publica WinFlow self-contained para `win-x64`;
3. crea una carpeta temporal de staging;
4. genera `AppxManifest.xml` con la identidad de Microsoft Store;
5. localiza `MakeAppx.exe` dentro de Windows SDK;
6. produce `msix-output/WinFlow_<version>_x64.msix`.

`msix-staging/`, `msix-output/` y `packaging/Assets/` están ignorados por Git.

## Firma local de prueba

El MSIX destinado a Partner Center queda sin firma local. Para instalar y probar una copia antes de enviarla a Microsoft Store, ejecuta desde la raíz del repositorio:

```powershell
powershell -ExecutionPolicy Bypass -File .\packaging\SignMsixForTesting.ps1
```

El script:

1. busca o crea un certificado autofirmado de prueba cuyo `Subject` coincide exactamente con el `Publisher` del paquete;
2. exporta solo el certificado público a `msix-output/WinFlow-Test.cer`;
3. solicita elevación de administrador para confiar ese certificado en `LocalMachine\TrustedPeople`;
4. copia el MSIX original a `msix-output/WinFlow_<version>_x64-test.msix`;
5. firma únicamente esa copia con `SignTool.exe`.

No se exporta ninguna clave privada al repositorio ni a `msix-output`. El certificado con clave privada permanece en `Cert:\CurrentUser\My` del PC de desarrollo. El archivo `WinFlow_<version>_x64.msix` original permanece intacto para Partner Center.

El certificado autofirmado es exclusivamente para pruebas locales. Microsoft Store firma el paquete de distribución después de la certificación.

## Pruebas antes de enviar

1. genera el MSIX con `BuildMsix.ps1`;
2. genera la copia firmada con `SignMsixForTesting.ps1`;
3. instala `WinFlow_<version>_x64-test.msix`;
4. comprueba `Alt+C` y `Alt+V`;
5. comprueba el icono de bandeja;
6. reinicia sesión y verifica el startup task;
7. comprueba la desinstalación desde Configuración;
8. ejecuta Windows App Certification Kit;
9. sube el MSIX original sin firma local a Partner Center.

## Archivos

- `Package.appxmanifest.template`: manifest base para Microsoft Store.
- `GenerateAssets.ps1`: genera los iconos requeridos por MSIX.
- `BuildMsix.ps1`: compila y empaqueta WinFlow.
- `SignMsixForTesting.ps1`: crea una copia firmada y confiable solo para pruebas locales.
