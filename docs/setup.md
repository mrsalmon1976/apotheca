# Development Environment Setup

## Firebase

- Add secrets file to `C:\Users\matt\.firebase\apotheca-dev-key.json`
  - To get the key file: Firebase Console → Project settings → Service accounts → Generate new private key → save the downloaded JSON to the path above (or wherever you prefer).
- Run 
  ```
  cd source/web-api/Apotheca.Api
  dotnet user-secrets set "Firebase:CredentialsPath" "C:/Users/matt/.firebase/apotheca-dev-key.json"
  ```
  
