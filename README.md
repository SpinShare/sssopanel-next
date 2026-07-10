# SSSOPanel Next
A modern desktop streaming overlay for SpinShare SpeenOpen

## Overview
- [Setup](#setup)
- [Building](#building)

### Setup
#### Installing all dependencies
```sh
cd SSSOPanel/UserInterface && npm install
```

#### Running a dev server
```sh
cd SSSOPanel/UserInterface && npm run dev
```

#### Starting the client
In a seperate terminal
```sh
cd SSSOPanel && dotnet run
```

### Building
#### Building the UI
```sh
cd SSSOPanel/UserInterface && npm run build
```

#### Building the application
```sh
cd SSSOPanel && dotnet build --configuration Release
```

 
