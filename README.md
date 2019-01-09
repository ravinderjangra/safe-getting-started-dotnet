# Safe-getting-started-dotnet

This repository contains .Net Framework and Xamarin.Forms (Android, iOS) example showcasing various features of the SAFE Network.

|SafeTodo Mobile Example| Console Desktop Example |
|:-:|:-:|
|[![Build Status](https://dev.azure.com/maidsafe/DevHub%20Example/_apis/build/status/DevHub%20Example-Mobile-CI)](https://dev.azure.com/maidsafe/DevHub%20Example/_build/latest?definitionId=6)|[![Build Status](https://dev.azure.com/maidsafe/DevHub%20Example/_apis/build/status/DevHub%20Example-.NET%20Desktop-CI)](https://dev.azure.com/maidsafe/DevHub%20Example/_build/latest?definitionId=5)|

## Desktop example (.Net Framework)

### Features 

Demonstrates the usage of:
 - Use the [MaidSafe.SafeApp](https://www.nuget.org/packages/MaidSafe.SafeApp) NuGet package to build a SAFE app.
 - Authentication
     - Mock authentication 
     - Mock authentication using [mock-safe-browser](https://github.com/maidsafe/safe_browser/releases)
     - Test net authentication using [safe-browser](https://github.com/maidsafe/safe_browser/releases)
 - Mutable Data
     - Perform CRUD operations on mutable data

### Pre-requisites
If building using Visual Studio, you will need the following SDKs and workloads installed:

- Windows desktop development

### Supported Platforms
- Windows (x64)

## Mobile example (Xamarin.Forms)

### Features
* Usage of [MaidSafe.SafeApp](https://www.nuget.org/packages/MaidSafe.SafeApp) package:
Demonstrate how to use MaidSafe.SafeApp NuGet package to develop SAFE mobile apps for Android and iOS. 
* Authentication using the [SAFE Authenticator](https://github.com/maidsafe/safe-authenticator-mobile):
A SAFE application needs to be authorised by the user before being able to connect to the network, this is achieved by sending an authorisation request to the SAFE Authenticator.
* Mock & non-mock feature:
Developer can switch between mock and non mock library for testing and deployment.
* Mutable data operations:
Perform CRUD operations on Mutable data.
* Usage of app's container:
Store mutable data information in an app's default container. This can be used to retain and retrieve data used in the application. 

### Pre-requisites
[SAFE Authenticator mobile](https://github.com/maidsafe/safe-authenticator-mobile) is required to authenticate app to connect to the SAFE network.

If building on Visual Studio 2017, you will need the following SDKs and workloads installed:

- Xamarin

### Required SDK/Tools
- Android 9.0 SDK
- Xcode 10+

### Supported Platforms
- Android 4.4+ (armeabi-v7, x86_64)
- iOS 8+ (ARM64, x64)

## Further Help

Get your developer related questions clarified on [SAFE Dev Forum](https://forum.safedev.org/). If you're looking to share any other ideas or thoughts on the SAFE Network you can reach out on [SAFE Network Forum](https://safenetforum.org/)


## Contribution

Copyrights in the SAFE Network are retained by their contributors. No copyright assignment is required to contribute to this project.


## License

This SAFE Network library is dual-licensed under the Modified BSD ([LICENSE-BSD](LICENSE-BSD) https://opensource.org/licenses/BSD-3-Clause) or the MIT license ([LICENSE-MIT](LICENSE-MIT) https://opensource.org/licenses/MIT) at your option.
