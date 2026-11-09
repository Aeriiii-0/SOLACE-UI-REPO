# Deploying Updates via GitHub for Solace

This setup connects **Solace** directly to your GitHub repository (**Aeriiii-0/SOLACE-UI-REPO**). Every time you publish a new release on GitHub, all installed desktop clients will automatically detect and download it.

---

## How It Works With GitHub

1. **Manifest File**: `SOLUM-UI.exe` reads:
   `https://raw.githubusercontent.com/Aeriiii-0/SOLACE-UI-REPO/master/deploy/update.xml`
2. **Changelog**: `SOLUM-UI.exe` displays release notes from:
   `https://raw.githubusercontent.com/Aeriiii-0/SOLACE-UI-REPO/master/deploy/changelog.html`
3. **Download**: When the user clicks **Update Now**, it downloads the release archive directly from:
   `https://github.com/Aeriiii-0/SOLACE-UI-REPO/releases/download/v{version}/Solace-v{version}.zip`

---

## Step-by-Step: Publishing a New Update

### Step 1: Bump the App Version
Open [`Properties/AssemblyInfo.cs`](file:///c:/Users/Renzo/Desktop/Solum/SOLACE-UI-REPO/Properties/AssemblyInfo.cs) and increment the version (e.g., from `1.0.0.0` to `1.0.1.0`):
```csharp
[assembly: AssemblyVersion("1.0.1.0")]
[assembly: AssemblyFileVersion("1.0.1.0")]
```

---

### Step 2: Build the Release Package
Run this in PowerShell from your project root:
```powershell
# 1. Build the Release version
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" SOLUM-UI.csproj /t:Build /p:Configuration=Release /v:minimal

# 2. Compress the output into a ZIP file (ready for GitHub Releases)
Compress-Archive -Path bin\Release\* -DestinationPath Solace-v1.0.1.zip -Force
```

---

### Step 3: Create a GitHub Release
1. Open your repository: **https://github.com/Aeriiii-0/SOLACE-UI-REPO/releases**
2. Click **"Draft a new release"**.
3. Set **Tag version**: `v1.0.1` (make sure it has the letter `v`).
4. Set **Release title**: `Solace v1.0.1`.
5. Attach the **`Solace-v1.0.1.zip`** file generated in Step 2 to the release assets.
6. Click **"Publish release"**.

---

### Step 4: Update `update.xml` and `changelog.html`
In your local repository:
1. Open [`deploy/update.xml`](file:///c:/Users/Renzo/Desktop/Solum/SOLACE-UI-REPO/deploy/update.xml):
   ```xml
   <?xml version="1.0" encoding="utf-8"?>
   <item>
       <version>1.0.1</version>
       <url>https://github.com/Aeriiii-0/SOLACE-UI-REPO/releases/download/v1.0.1/Solace-v1.0.1.zip</url>
       <changelog>https://raw.githubusercontent.com/Aeriiii-0/SOLACE-UI-REPO/master/deploy/changelog.html</changelog>
       <mandatory mode="2">false</mandatory>
   </item>
   ```
2. Open [`deploy/changelog.html`](file:///c:/Users/Renzo/Desktop/Solum/SOLACE-UI-REPO/deploy/changelog.html) and add your bullet points of what changed.
3. Commit and push:
   ```bash
   git add deploy/
   git commit -m "chore(release): publish v1.0.1 update manifest"
   git push origin master
   ```

---

### Step 5: That's It!
The moment `deploy/update.xml` is pushed to GitHub:
- Any user opening Solace will see the **"New Update Available"** window with your changelog.
- Clicking **Update** will download the zip, replace the files, and restart the app automatically in ~5 seconds.
