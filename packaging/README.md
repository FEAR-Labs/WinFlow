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

## Pendiente antes de generar el MSIX final

1. Reservar `WinFlow` en Microsoft Partner Center.
2. Copiar desde Partner Center los valores exactos de Package Identity Name y Publisher.
3. Sustituir `__PACKAGE_IDENTITY_NAME__` y `__PACKAGE_PUBLISHER__` en `Package.appxmanifest.template`.
4. Generar los recursos PNG de Store/MSIX requeridos por el manifest.
5. Publicar WinFlow para `win-x64` en una carpeta de staging.
6. Copiar el manifest final y los Assets al staging.
7. Ejecutar `makeappx.exe pack` para producir el `.msix` de prueba.
8. Firmar localmente el MSIX de prueba con un certificado cuyo Subject coincida con Publisher, o instalarlo mediante el flujo de desarrollo apropiado.
9. Ejecutar Windows App Certification Kit.
10. Subir el paquete certificado a Partner Center.

## Importante

`Package.appxmanifest.template` es deliberadamente una plantilla. No se deben inventar los valores de identidad de Microsoft Store: deben ser exactamente los entregados por Partner Center para WinFlow.
