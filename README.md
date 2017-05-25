Template for The Gamma REST services
====================================

After copying the project, rename the solution to your own name:

 * Rename `suave-service-template.sln` to whatever you wish and change 
   `build` target in `build.fsx` to use your new file name

To add a new service to the project:

 * Add services as `fsx` files in `src/servers` and add them 
   to `src/services.fsproj` (find the section containing `demo.fsx`).
   The order matter.

 * Go to `release.fs` and add your services to the compiled project 
   (to be run on Azure) by modifying the `[ "demo", Services.Demo.app ]` 
   part of the code.
