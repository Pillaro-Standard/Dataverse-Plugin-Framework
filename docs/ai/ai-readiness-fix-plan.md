# Plán oprav a zlepšení před zavedením AI instrukcí

> [!IMPORTANT]
> Tento dokument je **prováděcí plán**. Navazuje na
> [Analýzu AI instrukcí](./ai-instructions-analysis.md) a řeší jednu otázku:
> *co je potřeba v repozitáři opravit a zlepšit, aby AI instrukce fungovaly co nejlépe.*
>
> Každá položka má ověřený nález (soubor a řádek), důvod, proč AI rozbíjí, konkrétní opravu
> a kritérium hotovo. Všechny nálezy jsou verifikované proti zdrojovému kódu, ne odvozené
> z dokumentace.

---

## 📑 Obsah

- [1. Princip prioritizace](#1-princip-prioritizace)
- [2. Souhrn](#2-souhrn)
- [3. F1 — Blokující opravy: dokumentace učí nefunkční kód](#3-f1--blokující-opravy-dokumentace-učí-nefunkční-kód)
- [4. F2 — Kanonické vzory: jediná forma pro každý úkon](#4-f2--kanonické-vzory-jediná-forma-pro-každý-úkon)
- [5. F3 — Strojová ověřitelnost: zprovoznit, co už existuje](#5-f3--strojová-ověřitelnost-zprovoznit-co-už-existuje)
- [6. F4 — Formát dokumentace](#6-f4--formát-dokumentace)
- [7. F5 — Chybějící konvence a doplňky](#7-f5--chybějící-konvence-a-doplňky)
- [8. F6 — Zlepšení nad rámec oprav](#8-f6--zlepšení-nad-rámec-oprav)
- [9. Co záměrně nedělat](#9-co-záměrně-nedělat)
- [10. Doporučené pořadí a odhad](#10-doporučené-pořadí-a-odhad)
- [11. Rizika](#11-rizika)
- [12. Rozhodnutí, která potřebuji od zadavatele](#12-rozhodnutí-která-potřebuji-od-zadavatele)

---

## 1. Princip prioritizace

Pořadí není podle náročnosti, ale podle toho, **jak která vada degraduje AI výstup**:

| Třída | Co to je | Proč je to takto vysoko |
|---|---|---|
| **A. Nesprávnost** | Dokumentace popisuje API nebo chování, které v kódu neexistuje | Model to nemá jak poznat. Vygeneruje nekompilovatelný kód nebo tvrdí nepravdu o runtime chování. Nejdražší typ vady — vývojář ztrácí důvěru v AI i v dokumentaci. |
| **B. Nekonzistence** | Pro tentýž úkon existuje více forem, žádná není označená jako správná | Model kopíruje tu nejbližší. Výstup je nekonzistentní bez ohledu na kvalitu instrukcí — pravidly to nelze přebít, protože kód v `/examples` má větší váhu než próza. |
| **C. Neověřitelnost** | AI nemá jak zjistit, že se spletla | Bez gate se chyba dostane až k člověku. Každá ověřovací smyčka, kterou AI zvládne sama, snižuje objem manuálního review. |
| **D. Formát** | Bloky kódu bez jazyka, rozbité odkazy, duplicitní číslování | Zhoršuje čtení kontextu; efekt je reálný, ale menší než A–C. |
| **E. Mezery** | Konvence, které nejsou nikde zapsané | Model si je vymyslí. Konzistentně špatně napříč projekty. |

**Klíčové rozhodnutí:** F1–F3 je potřeba udělat **před** napsáním instrukcí. Instrukce
odkazující na nesprávnou dokumentaci konzervují chybu a je pak dražší ji vyndat.
F4–F6 lze dělat paralelně s instrukcemi.

---

## 2. Souhrn

| ID | Oblast | Třída | Náročnost | Blokuje instrukce? |
|---|---|---|---|---|
| F1-01 | `validation.md` — nekompilovatelný příklad (`PluginContext`) | A | S | ✅ |
| F1-02 | Dokumentace i XML doc popisují `DataverseValidationException` jinak než kód | A | S | ✅ |
| F1-03 | `plugin-registration-api.md` — nepřesné pravidlo unikátnosti názvů images | A | S | ✅ |
| F1-04 | Dokumentace generuje early-bound do `Plugins`, patří do `Logic` | A | M | ✅ |
| F1-05 | `CONTRIBUTING.md` — neexistující název solution souboru | A | S | ✅ |
| F1-06 | `docs/README.md` — rozbitý odkaz `VERSIONING.md` | A/D | S | — |
| F1-07 | `examples/TaskPlugin.cs` — nezarovnaná validace a filtering attributes | A/B | S | ✅ |
| F1-08 | `plugin-registration-api.md` — chybná reference „examples above“ | D | S | — |
| F1-09 | `getting-started.md` — dvakrát sekce `### 4.3` | D | S | — |
| F2-01 | **Jedna kanonická forma názvu atributu** — hotovo | B | M | ✅ |
| F2-02 | Jedna forma konstruktoru tasku | B | S | ✅ |
| F2-03 | Kolekce: `["Create"]` vs `new[] { "Create" }` | B | S | ✅ |
| F2-04 | Porovnání message v predikátech | B | S | ✅ |
| F2-05 | Přístup k pre-image: `PreImage` vs `GetPreImage()` | B | S | ✅ |
| F2-06 | Výběr atributů v registraci: typovaně vs. stringy — vyřešeno v F2-01 | B | M | ✅ |
| F3-01 | **Zprovoznit `validate` a `manifest` v CLI routeru** | C | S | ✅ |
| F3-02 | `-warnaserror` profil pro AI/CI běh | C | S | — |
| F3-03 | Odmítnutí GUIDů z dokumentace a příkladů ve validátoru | C | M | — |
| F3-04 | `docs/ai/verify.md` — doslovné příkazy ověření | C | M | ✅ |
| F3-05 | Pojistka identity dev prostředí (`PF-ENV-006`) | C | M | — |
| F4-01 | Normalizace bloků kódu (88 bloků bez jazyka) | D | L | — |
| F4-02 | Jeden typ fence (`~~~` → ```` ``` ````) | D | M | — |
| F5-01 | Konvence pojmenování stepů — hotovo | E | S | ✅ |
| F5-02 | Konvence pojmenování tasků a testů | E | S | ✅ |
| F5-03 | Hranice `Features/` vs. privátní metoda | E | S | — |
| F5-04 | Chybějící „co AI nesmí“ na úrovni souborů (ownership) | E | S | ✅ |
| F6-01 | `pillaro new-step` — generátor step/image ID | zlepšení | L | ✅ (dle Q3) |
| F6-02 | Kompilační test doc příkladů | zlepšení | L | — |
| F6-03 | Diagnostic Log → čtecí cesta pro agenta | zlepšení | L | — |
| F6-04 | Golden set / evals | zlepšení | M | — |

**Dvě věci, které plán nejvíc zlevnily proti původnímu odhadu:**

1. **Offline validace registračních metadat už je naprogramovaná** — chybí jen tři řádky
   v routeru (F3-01). V analýze jsem to odhadoval jako novou funkcionalitu.
2. Naopak přibyly dva rozpory dokumentace vs. kód (F1-02, F1-04), které jsem našel
   až při ověřování API proti zdrojáku. Bez jejich opravy by instrukce šířily nepravdu.

---

## 3. F1 — Blokující opravy: dokumentace učí nefunkční kód

### F1-01 · Příklad ve `validation.md` nejde zkompilovat · **S**

**Nález:** `docs/plugins/validation.md:240`

```csharp
.HasPreImageWhen(x => x.PluginContext.MessageName == "Update")
```

Lambda parametr je `TaskContext` (`IBasicImageValidation.HasPreImageWhen(Func<TaskContext, bool>, string)`).
`TaskContext` (`src/…/Tasks/TaskContext.cs:11–29`) má `PluginExecutionContext` a `Message` —
**vlastnost `PluginContext` neexistuje**. Je to jediný výskyt v celém repozitáři, takže
jde o překlep, ne o zastaralé API.

**Proč to AI rozbíjí:** `validation.md` je hlavní referenční příklad validačního řetězu.
Model ho zkopíruje včetně `x.PluginContext.MessageName` a vývojář dostane chybu kompilace
na nejfrekventovanějším vzoru celého frameworku.

**Oprava:** `x => x.Message == "Update"` (forma dle rozhodnutí F2-04).

**Hotovo když:** grep `PluginContext` v `/docs` nevrací nic a příklad je tvarově totožný
s `examples/…/Tasks/Task/SummarySync.cs:19`.

---

### F1-02 · Dokumentace i XML doc popisují `DataverseValidationException` jinak než kód · **S**

Nejzávažnější nález plánu, protože jde o **nejdůležitější behaviorální kontrakt frameworku** —
jak signalizovat business zamítnutí uživateli.

| Zdroj | Tvrzení |
|---|---|
| `src/…/Tasks/TaskBase.cs:82–88` | `TaskStatus.Success` + `LogSeverity.Info` — **správně** |
| `docs/plugins/execution-pipeline.md` | `Success` + `Info` — **správně** |
| `docs/plugins/task-model.md:271–274` | `TaskStatus.NotValid` + `LogSeverity.Info` — **špatně ve stavu** |
| `src/…/FluentInterfaces/IBreakValidation.cs` (XML doc `ThrowWithWarning`, obě přetížení) | „This error will be logged as **Warning**“ — **špatně** |
| `examples/…/Tasks/Contact/ValidateNames.cs:33` | komentář „will be logged as **warning**“ — **špatně** |

> [!IMPORTANT]
> **Rozhodnuto (D3): kód je správně, opravuje se dokumentace.**
> `ThrowWithWarning(...)` je informace pro uživatele, která se zároveň zapíše do logu.
> Task **splnil to, co měl** — vyhodnotil business pravidlo a výsledek oznámil uživateli —
> takže stav je `Success`. Záznam v logu je čistě informativní, takže severita je `Info`.
> **Slovo „warning“ v názvu metody popisuje povahu hlášky pro uživatele, ne úroveň logování.**

**Proč to AI rozbíjí:** model bude vývojáři tvrdit nepravdu o tom, jak se výsledek objeví
v monitoringu — a přesně kvůli monitoringu ta výjimka existuje. XML dokumentace navíc jde
do IntelliSense i do NuGet balíčku, takže se nepravda šíří k zákazníkům.

**Oprava — hotovo, součást této větve:**

1. `TaskBase.cs` — nad `catch (DataverseValidationException)` doplněn komentář, který kontrakt
   vysvětluje na místě, kde se o něm rozhoduje: proč `Success`, proč `Info`, a že „warning“
   se váže k hlášce pro uživatele, ne k logu.
2. `IBreakValidation.cs` — XML doc obou přetížení `ThrowWithWarning(...)` přepsán: už netvrdí
   „logged as Warning“, ale vysvětluje význam slova „warning“ a odkazuje na `ThrowWithError(...)`
   pro skutečná selhání (ověřeno v `ThrowExceptionValidator.cs:35–38`: `ThrowWithError`
   vyhazuje `InvalidPluginExecutionException`, tedy `Error` + `Error`).
3. `DataverseValidationException.cs` — doplněna XML dokumentace typu se stejným vysvětlením.
   Typ dosud žádnou neměl, přitom je to veřejné API, které vývojář volá nejčastěji.
4. `examples/…/ValidateNames.cs` — oba zavádějící komentáře nahrazeny.
5. `docs/plugins/task-model.md:271–274` — `TaskStatus.NotValid` → `TaskStatus.Success`
   plus jedna věta proč.

**Zbývá:** test, který kontrakt zafixuje pro oba vstupy (přímý `throw` v `DoExecute()`
i `ThrowWithWarning(...)` ve validačním řetězu), aby se rozpor nemohl vrátit.
Do pravidel patří i **negativní** varianta: `InvalidPluginExecutionException` pro business
zamítnutí vytvoří falešný `Error` v monitoringu — právě proto `DataverseValidationException`
existuje.

**Hotovo když:** kód, XML dokumentace, příklad i oba dokumenty tvrdí `Success` + `Info`
a existuje test, který to fixuje.

### F1-03 · Pravidlo unikátnosti názvů images je nepřesné · **S**

**Nález:** `docs/plugins/plugin-registration-api.md` (Validation Rules) tvrdí
*„image names must be unique within a step“*. Validátor
(`tools/…/PluginCommands/PluginManifestValidator.cs:134`) hlásí
`Duplicate {image.Type} image named '{image.Name}' in step '{step.StepId}'` — kontroluje se
tedy unikátnost **v rámci typu** image na stepu. Vlastní vzorový kód
(`examples/…/Plugins/TaskPlugin.cs`) proto legitimně používá název `"image"`
pro pre-image i post-image téhož stepu.

**Proč to AI rozbíjí:** model uvidí rozpor mezi pravidlem a vzorem. Buď začne vymýšlet
názvy typu `"preimage"` / `"postimage"` (a rozejde se s validací v `SummarySync`, která
čte default `"image"`), nebo bude hlásit chybu tam, kde žádná není.

**Oprava:** přepsat pravidlo na *„image names must be unique per image type within a step“*
a doplnit, že default název je `"image"` a **musí odpovídat názvu, který task očekává**
v `HasPreImage(...)` / `HasPostImage(...)`. Tuhle vazbu dnes dokumentace nezmiňuje vůbec,
přitom je to častý zdroj chyb.

**Hotovo když:** pravidlo odpovídá validátoru a je doplněná vazba registrace ↔ validace na názvu image.

---

### F1-04 · Dokumentace generuje early-bound do `Plugins`, patří do `Logic` · **M**

**Nález:** dokumentace si protiřečí ve věci s reálnými technickými důsledky.

| Zdroj | Implikace |
|---|---|
| `docs/plugins/early-bound-generation.md:20` | „Install … into the Dataverse **plugin project**“ |
| `docs/plugins/early-bound-generation.md:115` | „Run the generated wrapper from the **plugin project root**“ |
| `docs/plugins/early-bound-generation.md:84` | doporučený namespace `YourSolution.**Logic**.EarlyBound` |
| `docs/plugins/getting-started.md` §6, §6.3 | `Tools/` se generují v `Plugins`, odtud se spouští generování |
| `docs/plugins/architecture.md` | `Plugins` je jen shell; testy referencují `Logic` |
| `examples/…Examples.Logic/EarlyBound/` | early-bound klasy jsou v `Logic` |

> [!IMPORTANT]
> **Rozhodnuto (D1): early-bound klasy patří do `Logic`.** V šabloně a v příkladech nejsou
> commitnuté proto, že jsou závislé na konkrétním prostředí — generují se popsaným toolingem.

**Technické potvrzení, že `Logic` je i zamýšlený cíl toolingu.** Ověřeno v
`src/…/Tools/Deployment/Pillaro.Dataverse.PluginFramework.targets`:

- target `PillaroScaffoldEarlyBound` (ř. 157–159) je podmíněný jen vlastností
  `PillaroGenerateEarlyBoundTools` (default `true`), **není vázaný na typ projektu** — takže
  `Tools/EarlyBound/` se vygeneruje v každém projektu, který balíček referencuje. Podle
  `getting-started.md` §3.2 se framework balíček instaluje **i do `Logic`**, takže tooling
  tam už dnes je.
- generovaný `EarlyBoundSettings.json` má default `"namespace": "$(RootNamespace).EarlyBound"`
  (ř. 192). Správný namespace `YourSolution.Logic.EarlyBound` tedy vznikne **jen při spuštění
  z `Logic`**. Z `Plugins` by vyšel `YourSolution.Plugins.EarlyBound`.
- `<Compile Include="EarlyBound\**\*.cs" />` (ř. 35–36) kompiluje výstup do toho projektu,
  kde složka leží.

Dokumentace tedy neopisuje jiný záměr — jen říká špatný projekt.

**Proč to AI rozbíjí:** model podle `getting-started.md` vygeneruje early-bound do `Plugins`,
tasky v `Logic` na ně nedosáhnou, testy taky ne, a namespace nesedí s tím, co dokumentace
doporučuje. Tedy přesně to, čemu má architektonické rozdělení zabránit.

**Oprava:**

1. `early-bound-generation.md` — ř. 20 („plugin project“ → `Logic` projekt), ř. 115
   („plugin project root“ → root `Logic` projektu). Doplnit, že výstup `EarlyBound/`
   i `Tools/EarlyBound/` leží v `Logic`.
2. `getting-started.md` §6.3 — přesunout krok generování early-bound z kontextu `Plugins`
   do `Logic`; v §6 rozlišit, které `Tools/` patří kam (`ILMerge` + `Deployment` → `Plugins`,
   `EarlyBound` → `Logic`). Dnes to čte, jako by všechno patřilo do `Plugins`.
3. **Zpevnění (doporučeno):** v šabloně nastavit v `Plugins` projektu
   `<PillaroGenerateEarlyBoundTools>false</PillaroGenerateEarlyBoundTools>`. Tooling pak
   existuje na jediném místě a nelze ho omylem spustit z `Plugins`. Zabrání to chybě
   spolehlivěji než jakékoli pravidlo v instrukcích.
4. Do pravidel doplnit, že **`src/` není vzor**: framework vlastní `EarlyBound/` drží
   v `Pillaro.Dataverse.PluginFramework.Plugins`, protože v tomhle repu žádný `Logic`
   projekt neexistuje. Bez téhle poznámky si model vezme za vzor `src/`.

**Hotovo když:** dokumentace i šablona říkají shodně `Logic`, a projekt vytvořený ze šablony
nemá `Tools/EarlyBound/` v `Plugins`.

---

### F1-05 · `CONTRIBUTING.md` odkazuje na neexistující solution soubor · **S**

**Nález:** `docs/CONTRIBUTING.md` (Development Setup, krok 2) uvádí
`Pillaro.Dataverse.PluginFramework.sln`. Skutečný soubor je `Dataverse Plugin Framework.sln`
(mezery v názvu).

**Proč to AI rozbíjí:** agent postupující podle onboarding instrukcí selže na prvním kroku
a začne „hledat řešení“ — typicky vytvořením nového `.sln`, což je horší než nic.
Navíc mezery v názvu vyžadují uvozování v příkazech, což je samostatný zdroj chyb.

**Oprava:** opravit název; v `verify.md` (F3-04) uvádět cestu v uvozovkách.

**Hotovo když:** příkaz z dokumentace projde zkopírováním bez úprav.

---

### F1-06 · Rozbitý odkaz na `VERSIONING.md` · **S**

**Nález:** `docs/README.md:175` odkazuje `./VERSIONING.md`, soubor je `docs/versioning.md`.
Jediný rozbitý relativní odkaz v celém repozitáři (ověřeno kontrolou všech `.md` odkazů).

**Oprava:** `./versioning.md`.

**Hotovo když:** kontrola odkazů vrací nulu (a je součástí CI, viz F6-02).

---

### F1-07 · Vzorový plugin má nezarovnanou validaci a filtering attributes · **S**

**Nález:** `examples/…/Plugins/TaskPlugin.cs` (Update step, PostOperation) filtruje na
`regardingobjectid`, `scheduledend`, `statecode`, `statuscode`. Task
`examples/…/Tasks/Task/SummarySync.cs:22–28` ale validuje na
`RegardingObjectId`, `ScheduledEnd`, **`ScheduledStart`**, `StateCode`, `StatusCode`.
`scheduledstart` v registraci chybí, takže na jeho samostatnou změnu task nikdy nedostane
šanci se spustit.

**Proč to AI rozbíjí:** je to učebnicový příklad porušení PF-REG-001 (zarovnání
runtime registrace a deployment metadat) — a je přímo v referenční implementaci, kterou
model bere jako vzor správného řešení.

**Oprava:** srovnat obě strany (buď doplnit `scheduledstart` do `WhenChanged`, nebo ho
vyndat z validace — podle zamýšleného chování). Zároveň je to ideální **regresní test**
pro F6-02: kontrola, že množina validovaných atributů je podmnožinou filtering attributes.

**Hotovo když:** obě deklarace se shodují a nesoulad umí odhalit test.

---

### F1-08 · Nesprávná reference „examples above“ · **S**

`docs/plugins/plugin-registration-api.md` v sekci Example uvádí *„The examples **above** use
`Guid.Empty` placeholders intentionally“*, ale příklad následuje **až za** touto větou —
a nepoužívá `Guid.Empty`, nýbrž string `"00000000-0000-0000-0000-000000000000"`.
Model si z toho odvodí špatný kontext varování o placeholderech, což je ta poslední oblast,
kde chceme mít nejasnosti (viz GUID politika).

**Oprava:** přeformulovat na *„The example below uses all-zero GUID placeholders…“* a doplnit,
že takové ID validátor odmítne.

---

### F1-09 · Duplicitní číslování sekcí · **S**

`docs/plugins/getting-started.md` má dvě sekce `### 4.3` (*Add reference to the Logic project*
a *Install the plugin package*). Přečíslovat na 4.3 a 4.4 a srovnat navigaci.
Drobnost, ale rozbíjí odkazování „udělej krok 4.3“ v promptu i v instrukcích.

---

## 4. F2 — Kanonické vzory: jediná forma pro každý úkon

Nejsilnější páka na kvalitu AI výstupu v celém plánu. Model nevybírá „nejlepší“ formu —
vybírá tu, kterou vidí. Dokud existují tři formy, dostaneme tři různé výstupy a žádné
pravidlo to nepřebije, protože **spustitelný kód v `/examples` má pro model větší váhu
než próza v `/docs`**.

Pravidlo pro celé F2: *jeden úkon = jedna forma, kanonizovaná v `/examples`, popsaná
v pravidlech, a nikde v repu neexistuje protipříklad.*

### F2-01 · Název atributu — tři formy · **M** · hotovo, součást této větve

Nejdůležitější položka F2. Změřený stav před opravou (`/examples` a `/templates`, bez `EarlyBound`):

| Forma | Výskytů | Kde |
|---|---|---|
| `nameof(ContextEntity.FirstName)` | ~24 jako název atributu | `ValidateNames`, `UpdateAddressLabel`, `SummarySync` |
| `"firstname"` (string literál) | 48 | `ContactPlugin`, `TaskPlugin`, `ExamplePlugin` |
| `Contact.Fields.FirstName` | **0** | nikde |

Forma, kterou dokumentace doporučuje, nebyla v repozitáři použitá ani jednou. Model se učí
ze spustitelného kódu víc než z prózy, takže produkoval ty dvě formy, které viděl.

#### Kde se název atributu vyskytuje a jak selže chyba

| Kontext | API | Typovaná varianta | Jak selže špatný název |
|---|---|---|---|
| Validace v tasku | `EntityWithAtLeastOneAttribute`, `EntityWithAllAttributes` | ❌ jen `string[]` | **Tiše za běhu** |
| Registrace Update stepu | `WhenChanged`, `WithPreImage`, `WithPostImage` | ✅ `Expression<Func<TEntity, object>>` | typed: výjimka při buildu manifestu; string: až Dataverse při deploy |
| Registrace Create / obecná | `WithFilteringAttributes(params string[])` | ❌ jen `string[]` | Dataverse při deploy (hlasitě) |
| Čtení a zápis hodnot | typovaná property `ContextEntity.FirstName` | ✅ | nepřeloží se |

Ověřeno v `IPluginRegistration.cs:93,109,115,119,123` a `TypedAttributeSelector.cs`.
První řádek je klíčový: **validace je jediný kontext bez typované varianty a s tichým
selháním** — a přitom ji má každý task.

#### Proč `nameof(...)` zakázat

Argument není frekvence. Změřeno na třech vzorových entitách: ze 459 property
s `AttributeLogicalNameAttribute` se `property.ToLower()` rozchází s logickým názvem jen
u tří — u `Id` (`contactid`, `activityid`, `accountid`). Plus 8 relationship navigation
property v `contact.cs`, které nejsou atributy vůbec. Enum property (`StateCode`,
`AccountRoleCode`) `AttributeLogicalName` nesou, takže jsou v pořádku.

Problém je **režim selhání**. `nameof(...)` se přeloží pro jakýkoli člen, takže tohle je
platný kód:

```csharp
.EntityWithAtLeastOneAttribute(ContextEntity, nameof(ContextEntity.Id))
```

Vyhodnotí se na `"id"`, což v `Attributes` nikdy není. Task skončí jako `NotValid` při každém
spuštění a v logu to vypadá stejně jako legitimně odfiltrovaný task. `nameof(...)` tedy
převádí compile-time garanci na tiché runtime nic.

#### Pravidlo je dvoustupňové (nový poznatek)

Šablona **žádné early-bound typy neobsahuje** — jsou závislé na prostředí a generují se
toolingem (rozhodnutí D1). `ExampleTask` proto používá `TaskBase<Entity>` a `ExamplePlugin`
string-based registraci; `Contact.Fields.FirstName` tam neexistuje. **Fallback na string
literály tedy není okrajový případ, ale výchozí stav každého nového projektu.** Pravidlo
proto musí znít:

> Dokud pro entitu nejsou vygenerované early-bound typy, používej logické názvy jako string
> literály. Jakmile typ existuje, používej `Entity.Fields.X`. `nameof(...)` jako název
> atributu nikdy.

#### Co bylo změněno

| Soubor | Změna |
|---|---|
| `ValidateNames.cs` | 4 výskyty `nameof` → `Logic.Contact.Fields.*`; odstraněny 2× `.ToLower()` |
| `UpdateAddressLabel.cs` | 14 výskytů `nameof` → `Logic.Contact.Fields.*`; odstraněn `.ToLowerInvariant()` v `GetValue` |
| `SummarySync.cs` | 5 výskytů `nameof` → `Logic.Task.Fields.*` |
| `ContactPlugin.cs` | 29 literálů → `Contact.Fields.*` |
| `TaskPlugin.cs` | 15 literálů → `Task.Fields.*` |
| `docs/plugins/validation.md` | literály → `Contact.Fields.*` (+ oprava F1-01) |
| `ExampleTask.cs`, `ExamplePlugin.cs` (šablona) | kód nezměněn, doplněn komentář vysvětlující dvoustupňové pravidlo |

`.ToLower()` a `.ToLowerInvariant()` v příkladech existovaly **jen proto**, aby dorovnaly
PascalCase, který `nameof` vyrábí. S konstantami konverze zmizela.

**Co zůstává povolené:** `nameof(...)` mimo roli názvu atributu — popisek v logu
(`AddLogMessageLine($"Updating {nameof(ContextEntity.Address1_Name)} …")`) a kategorie testu
(`[Trait("Category", nameof(SummarySync))]`). Pravidlo zakazuje `nameof` **jako název
atributu**, ne `nameof` obecně.

**Závislost:** `emitFieldsClasses: true` musí v `EarlyBoundSettings.json` zůstat, jinak se
kanonická forma nepřeloží. Ověřeno, že je to default v šabloně
(`EarlyBoundSettings.template.json:15`).

**Vedlejší přínos:** s konstantami stojí validace i filtering attributes ve stejném tvaru,
takže nesoulad z [F1-07](#f1-07--vzorový-plugin-má-nezarovnanou-validaci-a-filtering-attributes--s)
(`SummarySync` validuje `ScheduledStart`, `TaskPlugin` na něj nefiltruje) je teď vidět na
první pohled.

**Zbývá ověřit:** build ve Visual Studiu. V kontejneru není .NET SDK, takže změny nebyly
zkompilovány — jsou ale výhradně compile-time (konstanty místo `nameof` a literálů), takže
případná chyba se projeví okamžitě při buildu, ne za běhu.

### F2-02 · Konstruktor tasku · **S**
Primary constructor (`ValidateNames`, `SummarySync`, `AutoNumbering`, oba doc příklady)
vs. explicitní konstruktor (`UpdateAddressLabel`).
→ kanonicky **primary constructor**, `UpdateAddressLabel` přepsat.

### F2-03 · Zápis kolekcí · **S**
`["Create", "Update"]` (většina) vs. `new[] { "Create", "Update" }`
(`UpdateAddressLabel.cs:35`). Druhá forma jde proti `IDE0028` z vlastního
`CONTRIBUTING.md`, tedy proti pravidlu zero-warning.
→ kanonicky **collection expression**.

### F2-04 · Porovnání message v predikátech · **S**
`ctx.Message == "Update"` (`SummarySync.cs:19–20`) vs.
`ctx.Message.Equals("Update", StringComparison.InvariantCultureIgnoreCase)`
(`UpdateAddressLabel.cs:36`). Message name z Dataverse je stabilní, takže složitější forma
nic nepřináší.
→ kanonicky **`ctx.Message == "Update"`**; pokud se tým rozhodne pro case-insensitive
variantu, pak `StringComparison.Ordinal**IgnoreCase**` (ne `InvariantCulture`) a všude stejně.

### F2-05 · Přístup k pre-image · **S**
`TaskBase` nabízí property `PreImage` / `PostImage` (nastavené v `InitializeContextData`,
`TaskBase.cs:217,220`) i chráněné metody `GetPreImage(name, throwException)` /
`GetPostImage(...)` (`TaskBase.cs:153,162`). Dokumentace zmiňuje jen properties, příklad
`UpdateAddressLabel` používá metodu.
→ kanonicky **property** pro default image; metodu použít **jen** pro pojmenovaný image
nebo pro `throwException: false`. Rozdíl zdokumentovat — dnes v dokumentaci metody nejsou vůbec.

### F2-06 · Výběr atributů v registraci · **M**
Dokumentace preferuje typované výrazy (`c => c.FirstName`) nebo `Fields` konstanty, ale
**oba vzorové pluginy používají výhradně string literály**. Nejviditelnější rozpor mezi
doporučením a referenční implementací v celém repu.
→ kanonizovat jednu formu (doporučuji `Fields` konstanty pro soulad s F2-01) a přepsat
`ContactPlugin.cs` i `TaskPlugin.cs`.

---

## 5. F3 — Strojová ověřitelnost: zprovoznit, co už existuje

### F3-01 · Zprovoznit `validate` a `manifest` v CLI routeru · **S** · nejlepší poměr cena/přínos

**Nález:** v `tools/Pillaro.Dataverse.PluginFramework.Cli/PluginCommands/` jsou
implementované příkazy `PluginValidateCommand`, `PluginManifestCommand` a `PluginDiffCommand`,
ale `PluginCommandRouter.RunAsync` (`PluginCommandRouter.cs:13–20`) routuje **pouze `deploy`**.
Ostatní tři jsou nedosažitelný kód a v helpu nejsou.

Ověřená charakteristika příkazů:

| Příkaz | Vstup | Potřebuje Dataverse? | Exit kódy |
|---|---|---|---|
| `manifest` | `--assembly`, `--output` | **Ne** | 0 / 1 / 3 |
| `validate` | `--manifest` | **Ne** | 0 / 1 / 2 / 3 |
| `diff` | `--manifest` + connection | Ano | 0 / 1 / 2 / 3 |

**Proč je to zásadní:** `manifest` + `validate` jsou **hotový offline gate na nejrizikovější
oblast AI výstupu** (registrační metadata, §5.7 analýzy) — assembly → manifest → validace,
bez připojení, s nenulovým exit kódem. V analýze jsem to odhadoval jako novou funkcionalitu;
reálně jde o tři řádky v routeru a rozšíření helpu.

**Oprava:** doroutovat `manifest`, `validate`, `diff`, doplnit help a pokrýt testem
(vzor existuje: `tests/…/Tests/PluginCommands/PluginRegistrationDiscoveryTests.cs` běží offline).

**Hotovo když:** `pillaro-dv validate --manifest <p>` a `pillaro-dv manifest --assembly <p>`
fungují bez připojení a při chybě vrací nenulový exit code.

> [!NOTE]
> Předpokládám, že příkazy nejsou doroutované záměrně (rozpracovaná funkcionalita), ne kvůli
> skryté závislosti. Před zapojením je vhodné ověřit, že `PluginManifestFactory.CreateFromAssembly`
> funguje na netriviální assembly — pokud ne, položka roste z S na M.

### F3-02 · `-warnaserror` profil · **S**
`PF-BUILD-001` (nula warningů) je dnes jen text v `CONTRIBUTING.md`; `.editorconfig` naopak
několik diagnostik vypíná (`CS1591`, `CS0436`, `CS8766`, `CS8603`) a `Nullable` je zapnuté
jen v testech. Doporučení: build profil (nebo `Directory.Build.props` pro AI/CI běh)
s `TreatWarningsAsErrors`, aby pravidlo bylo vynutitelné, ne aspirační. Vypnuté diagnostiky
ponechat vypnuté (mají důvod), ale **explicitně to zdůvodnit** — jinak je model bude
navrhovat zapínat.

### F3-03 · Validátor musí odmítnout GUIDy z dokumentace · **M**
Validátor dnes odmítá `Guid.Empty` a placeholder vzory, ale **nikoli reálné GUIDy
z `/examples`** (`4e56ef4c-0e08-f111-8407-000d3ab261ac`, `f94d984d-0f31-f111-88b4-000d3ab2695d`, …).
Přesně tyhle hodnoty model zkopíruje jako první. Doplnit deny-list — tím se z pravidla
„nekopíruj GUIDy“ stane vynutitelný gate. Navazuje na GUID politiku (tooling, Q3).

### F3-04 · `docs/ai/verify.md` · **M**
Doslovné příkazy, ne popis: co spustit pro `Logic`, co pro testy, co pro validaci manifestu,
v jakém pořadí, jaký exit code znamená co a co dělat při jednotlivých typech selhání.
Bez tohohle souboru si model příkazy vymýšlí — a na Windows/`VSBuild` stacku se to nepovede.
Musí obsahovat i **hranici**: co ověřit nelze (funkční chování bez nasazení).

### F3-05 · Pojistka identity dev prostředí · **M**
Vyplývá z rozhodnutí Q4 (agent má dev prostředí). Implementovat `PF-ENV-006`: před
spuštěním integračních testů ověřit URL cílového prostředí proti očekávané hodnotě a při
neshodě skončit chybou. Bez toho je jediný špatně nastavený connection string rozdíl mezi
dev a produkcí.

---

## 6. F4 — Formát dokumentace

### F4-01 a F4-02 · Bloky kódu · **L + M**

Ověřený stav `/docs` (bez `docs/ai`):

| Typ bloku | Počet | S jazykovou značkou |
|---|---|---|
| ```` ``` ```` fence | 41 | 33 |
| `~~~` fence | 50 | 43 |
| Odsazený blok (4 mezery) | 73 | 0 (nelze) |
| **Celkem** | **164** | **76 (46 %)** |

**54 % bloků kódu nemá jazykovou značku** a 73 z nich není ani ve fence. Nejhůř dopadly
soubory, které AI potřebuje nejvíc: `getting-started.md` (30 odsazených bloků),
`data-service.md` (19), `logging.md` (4), `validation.md` (3), `task-model.md` (2),
`plugin-model.md` (3).

**Oprava:** všechno na ```` ``` ```` s jazykem (`csharp`, `json`, `powershell`, `bash`,
`text`, `mermaid`, `xml`). Doporučuji ve dvou krocích: F4-02 (`~~~` → ```` ``` ````,
mechanické) a pak F4-01 (odsazené → fenced, vyžaduje rozhodnout jazyk u každého bloku).

**Vedlejší přínos:** při té příležitosti projdou příklady kontrolou — právě takhle se našly
F1-01 a F1-07.

---

## 7. F5 — Chybějící konvence a doplňky

Bez nich si model vymyslí vlastní konvenci a bude ji držet konzistentně napříč projekty —
konzistentně jinak, než chce tým.

| ID | Co chybí | Co je vidět v repu | Náročnost |
|---|---|---|---|
| F5-01 | ~~Pojmenování stepů (`WithName`)~~ — **hotovo**, viz níže | zavedeno `{StepPrefix} {entity} {Message} {Stage} {Mode}` | S |
| F5-02 | Pojmenování tasků a testů | tasky imperativně (`ValidateNames`, `UpdateAddressLabel`, `SummarySync`); testy `Method_Condition_Expectation` | S |
| F5-03 | Kdy `Features/` a kdy privátní metoda | `CustomerForbiddenNameService` je jediný vzor | S |
| F5-04 | Ownership souborů — čeho se AI nesmí dotknout | `EarlyBound/**`, `Tools/**` (přegenerovává balíček), `key.snk`, `appsettings*.json`, `PillaroSettings.json`, `power-platform-solutions/**` (binární zipy) | S |

F5-04 je z nich nejdůležitější: `docs/plugins/early-bound-generation.md` má tabulku
*File Ownership*, ale pokrývá jen early-bound tooling. Pro agenta je potřeba úplný seznam —
generované soubory přepsané modelem se ztratí při dalším buildu a diagnostika toho je drahá.

---

### F5-01 · Konvence pojmenování stepů · **S** · hotovo, součást této větve

Před opravou existovalo **šest** různých stylů ve třinácti výskytech:

| Styl | Kde | Příklad |
|---|---|---|
| `Pillaro Examples {StageAbbr} {Message} {Entity}` | `/examples`, 7× | `Pillaro Examples PreVal Create Contact` |
| `Pillaro Example Plugin {StageAbbr} {Message} {Entity}` | šablona, 2× | `Pillaro Example Plugin PreVal Update Contact` |
| `{Stage} {Message}` | **produkční plugin frameworku** | `Post Operation pl_AutoNumbering_GetNewNumber` |
| volný business název | `step-configuration.md`, 3× | `Lead Integration`, `Account Validation` |
| placeholder | `plugin-model.md` | `My Custom Step Name` |
| `{Message} {Entity} {Stage} {Mode}` | fallback v kódu | `Update contact PreOperation Synchronous` |

#### Co `WithName` technicky dělá (ověřeno)

- **Je nepovinný a spravuje se opt-in.** `PluginRegistrationDiffCalculator.cs:191–199`:
  *„If WithName() was not explicitly set, do not manage the name - leave it as-is in Dataverse.“*
  Nenastavený název deployment vůbec neporovnává.
- **Fallback se použije jen při vytvoření stepu** (`DataverseRegistrationUpserter.cs:190–196`):
  `{Message} {Entity} {Stage} {Mode}`. Step vytvořený před změnou konvence si starý název ponese navždy.
- **Párování je podle `StepId`**, ne podle názvu → přejmenování je bezpečné, duplikát nevznikne.
- **Porovnání je case-insensitive a normalizované** → změna jen velikosti písmen nezpůsobí update.
- **Název stepu není v diagnostickém logu.** `Log` nese `TaskName`, `Entity`, `Message`, `Stage`,
  `Mode`, `Depth`, `CorrelationId`. Publikum názvu jsou lidé v Plugin Registration Tool a
  v seznamech komponent solution, ne diagnostika.

#### Rozhodnutí: koordináty, ne účel (D4)

Vlastní otázka nebyl slovosled, ale filozofie. V repozitáři se míchaly dvě neslučitelné:

- **koordináty** (`Pillaro Examples PreVal Create Contact`) — odvoditelné z fluent chainu, takže
  deterministické pro AI a **ověřitelné**: název musí odpovídat chainu, jinak je jedno z toho špatně;
- **účel** (`Lead Integration`) — informativnější, ale neodvoditelný a neverifikovatelný; model si
  ho vymyslí a dva vývojáři pojmenují totéž různě.

Zvoleny koordináty. Účel už nesou názvy plugin classy a tasku, které Plugin Registration Tool
zobrazuje nad stepem — duplikovat ho do názvu stepu znamená druhé místo, které se rozejde.

Formát: `{StepPrefix} {entity} {Message} {Stage} {Mode}`, u stepů bez primární entity
(custom API, custom action) bez entity: `{StepPrefix} {Message} {Stage} {Mode}`.
`{Stage}` a `{Mode}` plnými slovy shodně s fluent metodami, `{entity}` logický název.
Entita před message záměrně — v plochém abecedním seznamu se hledá častěji „všechno na kontaktu“
než „všechny Create“.

#### Co bylo změněno

| Soubor | Změna |
|---|---|
| `Plugins/PluginBase.cs` (examples, framework, šablona) | přidána `protected const string StepPrefix` — `"Pillaro Examples"`, `"Pillaro Framework"`, `"$safeprojectname$"` |
| `ContactPlugin.cs` | 4 názvy |
| `TaskPlugin.cs` | 3 názvy |
| `ExamplePlugin.cs` (šablona) | 2 názvy; prefix nově z názvu generovaného projektu, ne „Pillaro“ |
| `AutonumberingPlugin.cs` | 1 název — produkční step frameworku, varianta bez entity |
| `plugin-model.md`, `step-configuration.md` | 5 názvů (placeholdery a business názvy → koordináty) |
| `plugin-registration-api.md` | nová sekce **Step Naming** s konvencí, důsledky nenastaveného názvu a pravidlem pro kolize |

Šablona dostala prefix `$safeprojectname$` místo hardcoded `"Pillaro Example Plugin"` — nový projekt
má nést vlastní prefix, ne prefix dodavatele frameworku.

**Kolize:** dva stepy se stejnou čtveřicí vzniknou, jen když dvě plugin classy registrují totéž;
pravidlo pak říká připojit název plugin classy. Unikátnost názvů nic nevynucuje, párování je podle ID.

**Zbývá ověřit:** build ve Visual Studiu (v kontejneru není .NET SDK) a jednorázový deploy examples,
který dorovná názvy — v diffu se objeví jako `Name:` důvody u 9 stepů.


## 8. F6 — Zlepšení nad rámec oprav

| ID | Zlepšení | Přínos | Náročnost |
|---|---|---|---|
| F6-01 | `pillaro new-step` — generátor step/image ID | Prerekvizita zvolené GUID politiky (Q3). Bez něj se model vrátí k vymýšlení GUIDů. | L |
| F6-02 | Kompilační/lint test doc příkladů + kontrola odkazů v CI | Zabrání regresi typu F1-01 a F1-06. Extrakce ```` ```csharp ```` bloků a build proti frameworku. | L |
| F6-03 | Diagnostic Log → čtecí cesta pro agenta | Uzavře smyčku *log → oprava → test*. Minimálně dotaz na `Diagnostic Log` podle correlation id do souboru. | L |
| F6-04 | Golden set 8–12 zadání + metriky | Jediná ochrana proti tomu, aby „vylepšení“ instrukcí kvalitu snížilo. | M |

F6-02 má nejtrvalejší efekt: bez něj se dokumentace znovu rozejde s kódem a celé F1
budeme za rok dělat znovu.

---

## 9. Co záměrně nedělat

Aby plán nepřerostl. Tyto věci **nejsou** předpokladem funkčních AI instrukcí:

- **Nepřepisovat `/docs` na „AI formát“.** Dokumentace je pro lidi dobrá. AI dostane
  samostatná pravidla, ne přepsané dokumenty.
- **Nezavádět unit testy s mockovaným Dataverse.** Jde proti PF-TEST-001 a proti záměru
  frameworku. Offline gate = build + validace manifestu, ne mockování.
- **Nemigrovat CI z Azure DevOps na GitHub Actions.** Nesouvisí s AI instrukcemi.
  (Pozn.: YAML pipeline v rootu s `–` v názvu je pro agenta matoucí, protože
  `.github/workflows/` existuje taky — stačí to popsat v pravidlech, ne přestavovat.)
- **Neřešit zpětnou kompatibilitu XML dokumentace** u F1-02 nad rámec opravy textu.
- **Nepřepisovat `.editorconfig`** — vypnuté diagnostiky mají důvod; jen je zdůvodnit.

---

## 10. Doporučené pořadí a odhad

| Krok | Obsah | Odhad | Výstup |
|---|---|---|---|
| 1 | F1 (všech 9 položek; F1-02 hotová, F1-04 včetně úpravy šablony) | 0,5–1 den | Dokumentace i kód tvrdí totéž |
| 2 | F2-02 … F2-05 + srovnání `/examples` (F2-01 a F2-06 hotové) | 0,5–1 den | Jediná kanonická forma pro každý úkon |
| 3 | F3-01, F3-02, F3-04 | 0,5 dne | AI si umí sama ověřit build i registrační metadata |
| 4 | **Napsat instrukce** (P0 z analýzy: `AGENTS.md` + katalog pravidel) | 1–2 dny | Použitelná instrukční sada |
| 5 | F5-01 … F5-04, F3-03, F3-05 | 0,5–1 den | Doplněné konvence a bezpečnostní pojistky |
| 6 | F4-02, F4-01 | 1–1,5 dne | Normalizovaný formát dokumentace |
| 7 | F6-01, F6-02 | 2–3 dny | GUID generátor a ochrana proti regresi |
| 8 | F6-03, F6-04 | 2–3 dny | Uzavřená smyčka a měření |

**Kroky 1–4 jsou minimální rozumný scope** (~3–5 dní): po nich existují instrukce, které
odkazují na správnou dokumentaci, mají jednu kanonickou formu pro každý úkon a AI si
umí ověřit výsledek. Kroky 5–8 zvyšují kvalitu a snižují degradaci v čase.

Kroky 1, 2 a 6 se dotýkají stejných souborů — dělat je **sekvenčně**, ne paralelně,
jinak vzniknou konflikty v `/examples` a v `validation.md`.

---

## 11. Rizika

| Riziko | Dopad | Mitigace |
|---|---|---|
| F2 mění vzorový kód, který se buildí v PR pipeline | Rozbitý build examples | Po každé změně `/examples` build + nightly testy; změny po jednom tasku |
| Rozpor u `DataverseValidationException` se vrátí | Dokumentace se znovu rozejde s kódem na nejdůležitějším kontraktu | Kontrakt je nově vysvětlený přímo v `TaskBase.cs` a v XML dokumentaci typu; doplnit test |
| F3-01 může narazit na nedokončenou funkcionalitu | Ze S se stane M–L | Ověřit `CreateFromAssembly` na reálné assembly před zapojením |
| Instrukce se v čase rozejdou s frameworkem | Návrat do dnešního stavu | F6-02 + vlastník katalogu (otevřená otázka analýzy) |
| Agent má přístup do dev prostředí (Q4) | Nechtěný zápis do jiného prostředí | F3-05 před předáním credentials, nikdy ne obráceně |

---

## 12. Rozhodnutí, která potřebuji od zadavatele

Bez nich nelze některé položky dokončit. U každého je uvedené doporučení, takže lze
odsouhlasit „jdi s doporučením“.

### Rozhodnuto

| # | Rozhodnutí | Výsledek | Dopad |
|---|---|---|---|
| **D2** | Kanonická forma názvu atributu | **`Entity.Fields.X`**, jednotně ve všech kontextech. `nameof(...)` jako název atributu zakázán. String literály jsou správné, dokud early-bound typy neexistují — což je stav každého nového projektu. | F2-01 hotová; F2-06 tím vyřešena zároveň |
| **D4** | Konvence pojmenování stepů | **Koordináty, ne účel:** `{StepPrefix} {entity} {Message} {Stage} {Mode}`, bez entity u custom API/action. `WithName` vždy nastavit. Frameworkový autonumbering step přejmenován. | F5-01 hotová |
| **D1** | Kde leží early-bound klasy | **`Logic`.** V šabloně a příkladech nejsou commitnuté, protože závisí na prostředí — generují se toolingem. | F1-04 přepsáno; přidán návrh vypnout scaffolding v `Plugins` |
| **D3** | Chování `DataverseValidationException` | **Kód je správně: `Success` + `Info`.** „Warning“ v `ThrowWithWarning(...)` je povaha hlášky pro uživatele, ne úroveň logu; task splnil svou práci. | F1-02 zůstává opravou dokumentace (S), bez změny chování. Kontrakt je nově vysvětlený v kódu. |

### Zbývá rozhodnout

| # | Rozhodnutí | Doporučení | Blokuje |
|---|---|---|---|
| **D5** | `TreatWarningsAsErrors` — jen pro AI/CI profil, nebo pro všechny buildy? | **Jen AI/CI profil**, aby to nebrzdilo lokální rozpracovaný kód | F3-02 |
| **D6** | Smí se měnit vzorový kód v `/examples`? | Ano — pro AI je to autoritativnější zdroj než próza | celé F2 |

---

## ➡️ Související dokumenty

- [Analýza AI instrukcí](./ai-instructions-analysis.md)
- [Validation Model](../plugins/validation.md)
- [Task Model](../plugins/task-model.md)
- [Plugin Registration API](../plugins/plugin-registration-api.md)
- [Early-Bound Entity Generation](../plugins/early-bound-generation.md)
- [Contributing](../CONTRIBUTING.md)
