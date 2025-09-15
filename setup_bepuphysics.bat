@echo off
echo Cloning BepuPhysics2 repository into rubens-psx-engine folder...
git clone https://github.com/bepu/bepuphysics2

echo Checking out specific commit for consistency...
cd bepuphysics2
git checkout bfb11dc2020555b09978c473d9655509e844032c
cd ..

echo BepuPhysics2 setup complete!
echo Commit hash: bfb11dc2020555b09978c473d9655509e844032c
pause