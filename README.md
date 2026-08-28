<div align="center">

<img src="./assets/icons/WinFlow-Icon.svg" alt="WinFlow" width="96" height="96" />

# WinFlow

**Controla tus ventanas de forma rápida y simple.**

Una utilidad ligera para Windows que funciona en segundo plano y permite controlar la ventana activa con atajos globales.

<img width="800" height="450" alt="WinFlow en acción" src="./assets/gifts/Ordena.gif" />

</div>

---

## Descarga

La versión más reciente de WinFlow estará disponible en [GitHub Releases](https://github.com/FEAR-Labs/WinFlow/releases/latest).

Descarga `WinFlow.exe` y ejecútalo. No necesitas un instalador adicional.

## Atajos

### Alt + C — Centrar

Presiona **Alt + C** para centrar la ventana activa en el área de trabajo del monitor donde se encuentra.

<div align="center">

<img width="800" height="450" alt="Alt + C centrando una ventana" src="./assets/gifts/Alt~c.gif" />

</div>

### Alt + V — Maximizar / restaurar

Presiona **Alt + V** para maximizar la ventana activa.

<div align="center">

<img width="800" height="450" alt="Alt + V maximizando una ventana" src="./assets/gifts/Alt~v.gif" />

</div>

Si la ventana ya está maximizada, presiona **Alt + V** nuevamente para restaurarla y centrarla automáticamente.

---

## Bandeja del sistema

WinFlow funciona en segundo plano y muestra un icono en la bandeja del sistema.

Haz **clic derecho** sobre el icono para abrir el menú de WinFlow. Desde ahí puedes consultar los atajos disponibles o cerrar la aplicación.

## Instalación

Ejecuta `WinFlow.exe`.

WinFlow se instala automáticamente en `%LocalAppData%\Programs\WinFlow` y queda configurado para **iniciarse junto con Windows**.

Durante la instalación se muestra el progreso. Una vez terminada, el archivo original queda libre para que puedas eliminarlo de Descargas si quieres.

## Desinstalación

WinFlow se registra en Windows para poder desinstalarse normalmente.

Al desinstalarlo se eliminan la aplicación, sus accesos y la carpeta de instalación.

## Requisitos

- Windows 10 o Windows 11 de 64 bits.

## Comportamiento

WinFlow utiliza APIs estándar de Windows para centrar, restaurar y maximizar ventanas.

No modifica archivos del sistema ni la configuración de tus monitores. Las ventanas que no puedan ser controladas se ignoran. Si un atajo ya está registrado por otra aplicación, WinFlow continúa funcionando con los atajos que sí estén disponibles.

## Limitaciones

- Algunas aplicaciones o ventanas protegidas pueden impedir que herramientas externas cambien su posición o estado.
- Microsoft Smart App Control puede bloquear la versión actual de WinFlow o mostrar una advertencia indicando que no se puede verificar el editor. En algunos equipos, iniciar WinFlow mediante **“Ejecutar como administrador”** puede permitir que la aplicación se ejecute, aunque esto depende de la configuración y las políticas de seguridad de Windows. Estamos trabajando en una **versión de WinFlow para Microsoft Store**, con el objetivo de ofrecer una instalación más sencilla y correctamente distribuida a través de Microsoft, reduciendo este tipo de advertencias o bloqueos.
