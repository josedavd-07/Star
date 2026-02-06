# Star Language 🌟

**Star** is a modern, space-themed programming language designed for visual elegance and structural power. It offers a native experience with its own project system and a professional CLI.

## Quick Start (Hello Galaxy)

Building your first constellation is as simple as creating a file. Here is a functional example including classes:

```star
StarName MiAventura.Core;

// Declaración de una Constellation (Clase)
Constellation Explorador {
    Public String Nombre;
    Int Energia = 100; // Variable con valor inicial

    Public StarFunction Saludar() {
        EmitLn("🚀 Explorador " + this.Nombre + " reportándose con " + this.Energia + "% de energía.");
    }
}

StarFunction Main() {
    Explorador nova = new Explorador();
    nova.Nombre = "Nova";
    nova.Saludar();
}
```

## Guía de Lenguaje

### Variables y Tipos
Star utiliza tipos estáticos con una sintaxis clara:
- `Int`: Números enteros.
- `String`: Cadenas de texto.
- `Bool`: Valores lógicos.
- `Nova`: Representa la ausencia de valor (void).

```star
Int planetas = 8;
String sistema = "Solar";
Bool habitado = true;
```

### Constellations (Clases)
Las clases en Star se llaman **Constellations**, fomentando una organización estelar del código.
- **Acceso**: `Public`, `Private`, `Protected`.
- **Miembros**: Variables y `StarFunctions`.

## Instalación

### Linux (Recomendado)
Descarga el paquete `.deb` desde los releases y ejecútalo:
```bash
sudo dpkg -i star-language_1.0.0_amd64.deb
```
O utiliza el tarball `.tar.gz`:
```bash
tar -xzf star-language-v2.0.0-linux.tar.gz
cd star-language-v2.0.0-linux && ./install.sh
```

### Windows
Ejecuta el script de PowerShell proporcionado en la sección de releases:
```powershell
.\install-windows.ps1
```

## CLI: Comandos del Sistema
Una vez instalado, el comando `star` estará disponible globalmente:
- `star new console -name <proyecto>`: Crea un nuevo proyecto estructurado.
- `star run`: Ejecuta el proyecto actual (detecta automáticamente el `.starproj`).
- `star build`: Compila el proyecto en un binario ejecutable.
- `star --version`: Muestra la versión actual del compilador.

## VS Code Extension
Para la mejor experiencia, instala la extensión oficial de **Star Language** desde la Marketplace o vía VSIX.
- **Iconos Propios**: Reconocimiento de archivos `.starproj`.
- **Estética Premium**: Funciones en **negrita**, palabras clave en *cursiva*.

---
*Explora el cosmos del código con Star Language.*
