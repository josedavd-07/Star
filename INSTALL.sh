#!/bin/bash

# Colores para la salida
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${BLUE}🚀 Instalando Star Language...${NC}"

# 1. Compilar el compilador
if [ -f "./star" ]; then
    echo -e "${BLUE}[*] Compilando estación base...${NC}"
    chmod +x ./star
    ./star build
else
    echo -e "${YELLOW}[!] Error: No se encontró el disparador 'star'.${NC}"
    exit 1
fi

# 2. Instalar el binario en el PATH
echo -e "${GREEN}[+] Instalando binario en /usr/local/bin/star...${NC}"
# Copiamos el wrapper inicial o el binario si ya existe
if [ -f "./publish/StarCompiler" ]; then
    sudo cp ./publish/StarCompiler /usr/local/bin/star
else
    sudo cp ./star /usr/local/bin/star
fi
sudo chmod +x /usr/local/bin/star

# 3. Asegurar accesibilidad del PATH
if [[ ":$PATH:" != *":/usr/local/bin:"* ]]; then
    echo -e "${YELLOW}[!] /usr/local/bin no está en tu PATH. Agregándolo a .bashrc...${NC}"
    echo 'export PATH="$PATH:/usr/local/bin"' >> ~/.bashrc
    echo 'export PATH="$PATH:/usr/local/bin"' >> ~/.zshrc 2>/dev/null
    echo -e "${GREEN}[+] PATH actualizado. Reinicia tu terminal para aplicar los cambios.${NC}"
fi

# 4. Instalar Fuentes
echo -e "${GREEN}[+] Instalando fuentes SF Mono para la mejor experiencia...${NC}"
mkdir -p ~/.local/share/fonts
cp assets/fonts_collection/*.otf ~/.local/share/fonts/ 2>/dev/null
fc-cache -f -v > /dev/null

echo -e "\n${BLUE}✨ ¡Tu aventura galáctica ha sido configurada globalmente!${NC}"
echo -e "${BLUE}Ya puedes usar 'star' desde cualquier lugar.${NC}"
