#!/bin/bash
# Script to build a .deb package for Star Language

APP_NAME="star-language"
VERSION="1.0.0"
DEB_DIR="star-language_${VERSION}_amd64"

echo "Building .deb package..."

# 1. Prepare directory structure
mkdir -p ${DEB_DIR}/usr/local/bin
mkdir -p ${DEB_DIR}/usr/local/share/star-language/fonts
mkdir -p ${DEB_DIR}/DEBIAN

# 2. Copy binaries and assets
# Copy ALL files from the publish directory (not just the executable)
cp -r ./publish/* ${DEB_DIR}/usr/local/share/star-language/
chmod +x ${DEB_DIR}/usr/local/share/star-language/StarCompiler

# Create a wrapper script in /usr/local/bin that calls the actual binary
cat <<'WRAPPER' > ${DEB_DIR}/usr/local/bin/star
#!/bin/bash
exec /usr/local/share/star-language/StarCompiler "$@"
WRAPPER
chmod +x ${DEB_DIR}/usr/local/bin/star

cp assets/fonts_collection/*.otf ${DEB_DIR}/usr/local/share/star-language/fonts/

# 3. Create control file
cat <<EOF > ${DEB_DIR}/DEBIAN/control
Package: star-language
Version: ${VERSION}
Section: devel
Priority: optional
Architecture: amd64
Maintainer: Jose David Carranza Angarita
Description: Star Language Compiler and Tools
 A modern, space-themed programming language.
EOF

# 4. Create post-install script for fonts
cat <<EOF > ${DEB_DIR}/DEBIAN/postinst
#!/bin/bash
echo "Installing fonts..."
mkdir -p /usr/local/share/fonts/star-language
cp /usr/local/share/star-language/fonts/*.otf /usr/local/share/fonts/star-language/
fc-cache -f -v > /dev/null
EOF
chmod +x ${DEB_DIR}/DEBIAN/postinst

# 5. Build the package
dpkg-deb --build ${DEB_DIR}

echo "Done! Package created: ${DEB_DIR}.deb"
