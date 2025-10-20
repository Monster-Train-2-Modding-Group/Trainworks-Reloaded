# Trainworks-Reloaded
[![Ask DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/Monster-Train-2-Modding-Group/Trainworks-Reloaded)
[![Bugs](https://sonarcloud.io/api/project_badges/measure?project=Monster-Train-2-Modding-Group_Trainworks-Reloaded&metric=bugs)](https://sonarcloud.io/summary/new_code?id=Monster-Train-2-Modding-Group_Trainworks-Reloaded)
[![Code Smells](https://sonarcloud.io/api/project_badges/measure?project=Monster-Train-2-Modding-Group_Trainworks-Reloaded&metric=code_smells)](https://sonarcloud.io/summary/new_code?id=Monster-Train-2-Modding-Group_Trainworks-Reloaded)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=Monster-Train-2-Modding-Group_Trainworks-Reloaded&metric=coverage)](https://sonarcloud.io/summary/new_code?id=Monster-Train-2-Modding-Group_Trainworks-Reloaded)
[![Duplicated Lines (%)](https://sonarcloud.io/api/project_badges/measure?project=Monster-Train-2-Modding-Group_Trainworks-Reloaded&metric=duplicated_lines_density)](https://sonarcloud.io/summary/new_code?id=Monster-Train-2-Modding-Group_Trainworks-Reloaded)

Modding Framework for Monster Train 2

## How to Install

(Note: Trainworks Reloaded is built as a BepInEx 5 mod. Install version 5, not 6.)

1. [Install BepInEx into Monster Train 2](https://docs.bepinex.dev/articles/user_guide/installation/index.html)
2. Install our latest plugin and unzip it into the BepInEx plugins folder.
3. Install other mods similarly.

## How to Build

In order to build the project you will need access to the nuget package registry, run the following with your GITHUB_TOKEN.

`dotnet nuget add source --username $GITHUB_USERNAME --password $GITHUB_TOKEN --name monster-train-packages "https://nuget.pkg.github.com/Monster-Train-2-Modding-Group/index.json"`

From there it is buildable as regular C# project through Visual Studio.


## Creating Your First Mod

Check out the Wiki for an in-depth guide: https://github.com/Monster-Train-2-Modding-Group/Trainworks-Reloaded/wiki

But basically:
1. Create a public Github Repository
2. Go to  GitHub Settings → Developer Settings → Personal Access Tokens and generate a new token (classic) with read:packages and public_repo permissions
3. Go settings -> Codespaces -> Secrets and create a new secret 'GH_PACKAGES_TOKEN' with that new token, assign it to that token.
4. Go to Code and open up a Codespace
5. run `pip install cookiecutter`
6. run `cookiecutter https://github.com/Monster-Train-2-Modding-Group/Mod-Template.git`
7. Fill out the cookiecutter command.
7a. It will ask for a Namespace on Thunderstore, go ahead and make an account on thunderstore.com and create a namespace, elsewise you will need to customize the pipelines and thunderstore.toml later.
7b. after running the command move the files generated out of the folder and into the root namespace.
8. In the bottom left, github codespace will point out that there is a dev container change, press to rebuild. (do regular rebuild)
9. run `dotnet nuget update source --username $GITHUB_USER --password $GH_PACKAGES_TOKEN monster-train-packages --store-password-in-clear-text`
10. run `dotnet build`
11. you will have built your first mod, commit and save and we can do the first release.
12. Create a Github Release

## Release Guide

To make a Release for Trainworks-Reloaded, do the following:
1. Update the thunderstore.toml version to be your release tag.
2. Create a Git Tag with the corresponding semvar
