#!/usr/bin/env bash
# Compile le C# de l'application Windows (CeramiPro.App) sur une machine
# sans Windows, puis vérifie le XAML. Voir outils/verifier-wpf/.
set -euo pipefail

racine="$(cd "$(dirname "$0")/.." && pwd)"
projet="$racine/outils/verifier-wpf"

python3 "$projet/generer_parties_xaml.py" "$racine/src/CeramiPro.App" "$projet/obj/parties-xaml" > /dev/null
dotnet build "$projet/VerifierApplicationWindows.csproj" -v q --nologo
python3 "$racine/outils/verifier-xaml.py"
