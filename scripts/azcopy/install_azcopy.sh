# taken from https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azcopy-v10#install-azcopy
# Download the repository configuration package.
curl -sSL -O https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb

# Install the Microsoft repository configuration package.
sudo dpkg -i packages-microsoft-prod.deb

# Delete the repository configuration package after you've installed it.
rm packages-microsoft-prod.deb

# Update the package index files.
sudo apt-get update

# Install AzCopy.
sudo apt-get install -y azcopy
