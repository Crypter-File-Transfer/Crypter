From the .sln directory:
* docker-compose -f Crypter.API/docker-compose.yml build api
* docker build -f Crypter.API/Dockerfile -t crypter-api:\{version}
* docker-compose -f Crypter.API/docker-compose.yml up -d api