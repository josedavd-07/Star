#!/bin/bash
# Script to build a .tar.gz tarball for Star Language

VERSION="1.0.0"
DIST_DIR="star-language-v${VERSION}-linux"

echo "Creating tarball distribution..."

# 1. Prepare directory structure
mkdir -p ${DIST_DIR}/bin
mkdir -p ${DIST_DIR}/fonts

# 2. Copy binaries and assets
cp star_binary ${DIST_DIR}/bin/star
chmod +x ${DIST_DIR}/bin/star

cp assets/fonts_collection/*.otf ${DIST_DIR}/fonts/

# 3. Add a simple install script inside the tarball
cat <<EOF > ${DIST_DIR}/install.sh
#!/bin/bash
PROJECT_ROOT=\$(pwd)
echo "Setting up Star Language..."
sudo ln -sf \${PROJECT_ROOT}/bin/star /usr/local/bin/star
mkdir -p ~/.local/share/fonts
cp fonts/*.otf ~/.local/share/fonts/
fc-cache -f -v
echo "Done! You can now run 'star' from any terminal."
EOF
chmod +x ${DIST_DIR}/install.sh

# 4. Create the archive
tar -czvf ${DIST_DIR}.tar.gz ${DIST_DIR}

echo "Done! Tarball created: ${DIST_DIR}.tar.gz"
