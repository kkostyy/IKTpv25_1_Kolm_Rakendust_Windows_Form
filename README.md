# IKTpv25_1_Kolm_Rakendust_Windows_Form

Kolm Windows Forms rakendust ühes projektis (C#, .NET 8).

| # | Rakendus | Kirjeldus |
|---|----------|-----------|
| 1 | **Pildi vaataja** | Pildi valimine failidialoogiga, taustavärvi muutmine, tühjendamine, venitamine |
| 2 | **Math Quiz** | 30 sekundit, neli tehet (+, −, ×, ÷) juhuslike arvudega |
| 3 | **Matching Game** | 16 kaarti, leia kõik paarid; käikude ja aja loendus |

Peamenüü (`MainMenuForm`) käivitab kõik kolm rakendust.

## Käivitamine

Vajalik: Windows ja [.NET 8 SDK](https://dotnet.microsoft.com/download) (või Visual Studio 2022).

```bash
dotnet run --project IKTpv25_1_Kolm_Rakendust_Windows_Form
```

Või ava `IKTpv25_1_Kolm_Rakendust_Windows_Form.sln` Visual Studios ja vajuta F5.

## Struktuur

```
IKTpv25_1_Kolm_Rakendust_Windows_Form/
├── Program.cs
├── MainMenuForm.cs
├── PictureViewerForm.cs
├── MathQuizForm.cs
└── MatchingGameForm.cs
```
