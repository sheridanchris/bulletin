{
  inputs = {
    nixpkgs.url = "github:nixos/nixpkgs/master";
    flake-utils.url = "github:numtide/flake-utils";
    flake-compat = {
      url = "github:edolstra/flake-compat";
      flake = false;
    };
  };

  outputs = { self, nixpkgs, flake-utils, ... }:
  flake-utils.lib.eachSystem
    [ "x86_64-linux" ]
    (system:
    let
      pkgs = import nixpkgs {
        inherit system;
      };
    in 
    rec
    {
      devShell = pkgs.mkShell rec {
        buildInputs = with pkgs; [
          dotnet-sdk
        ];

        LD_LIBRARY_PATH = pkgs.lib.makeLibraryPath buildInputs;
      };
    });
}
