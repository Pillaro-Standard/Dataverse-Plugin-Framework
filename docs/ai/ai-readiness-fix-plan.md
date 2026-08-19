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
| F1-02 | `DataverseValidationException` loguje `Info`, má logovat `Warning` — **změna kódu** | A | M | ✅ |
| F1-03 | `plugin-registration-api.md` — nepřesné pravidlo unikátnosti názvů images | A | S | ✅ |
| F1-04 | Dokumentace generuje early-bound do `Plugins`, patří do `Logic` | A | M | ✅ |
| F1-05 | `CONTRIBUTING.md` — neexistující název solution souboru | A | S | ✅ |
| F1-06 | `docs/README.md` — rozbitý odkaz `VERSIONING.md` | A/D | S | — |
| F1-07 | `examples/TaskPlugin.cs` — nezarovnaná validace a filtering attributes | A/B | S | ✅ |
| F1-08 | `plugin-registration-api.md` — chybná reference „examples above“ | D | S | — |
| F1-09 | `getting-started.md` — dvakrát sekce `### 4.3` | D | S | — |
| F2-01 | **Jedna kanonická forma názvu atributu** | B | M | ✅ |
| F2-02 | Jedna forma konstruktoru tasku | B | S | ✅ |
| F2-03 | Kolekce: `["Create"]` vs `new[] { "Create" }` | B | S | ✅ |
| F2-04 | Porovnání message v predikátech | B | S | ✅ |
| F2-05 | Přístup k pre-image: `PreImage` vs `GetPreImage()` | B | S | ✅ |
| F2-06 | Výběr atributů v registraci: typovaně vs. stringy | B | M | ✅ |
| F3-01 | **Zprovoznit `validate` a `manifest` v CLI routeru** | C | S | ✅ |
| F3-02 | `-warnaserror` profil pro AI/CI běh | C | S | — |
| F3-03 | Odmítnutí GUIDů z dokumentace a příkladů ve validátoru | C | M | — |
| F3-04 | `docs/ai/verify.md` — doslovné příkazy ověření | C | M | ✅ |
| F3-05 | Pojistka identity dev prostředí (`PF-ENV-006`) | C | M | — |
| F4-01 | Normalizace bloků kódu (88 bloků bez jazyka) | D | L | — |
| F4-02 | Jeden typ fence (`~~~` → ```` ``` ````) | D | M | — |
| F5-01 | Konvence pojmenování stepů | E | S | ✅ |
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

### F1-02 · `DataverseValidationException` loguje `Info`, má logovat `Warning` · **M** · změna kódu

Nejzávažnější nález plánu, protože jde o **nejdůležitější behaviorální kontrakt frameworku** —
jak signalizovat business zamítnutí uživateli. Nález byl původně čtyřcestný rozpor:

| Zdroj | Tvrzení |
|---|---|
| `src/…/Tasks/TaskBase.cs:82–88` | `Log.Status = TaskStatus.Success`, `Log.LogSeverity = LogSeverity.Info` |
| `docs/plugins/execution-pipeline.md` | `Success` + `Info` |
| `docs/plugins/task-model.md:271–274` | `TaskStatus.NotValid` + `LogSeverity.Info` |
| `src/…/FluentInterfaces/IBreakValidation.cs` (XML doc `ThrowWithWarning`, obě přetížení) | „will be logged as **Warning**“ |
| `examples/…/Tasks/Contact/ValidateNames.cs:33` | komentář „will be logged as **warning**“ |

> [!IMPORTANT]
> **Rozhodnuto (D3): správná je `Warning`.** Chování se musí shodovat funkčně i logicky —
> metoda `ThrowWithWarning(...)` nemůže logovat `Info`. Zdrojem pravdy tedy **není kód**;
> opravuje se kód, ne dokumentace.

**Proč to AI rozbíjí:** dva různé důsledky, oba drahé. (a) Model tvrdí nepravdu o tom, jak
se výsledek objeví v monitoringu — a přesně kvůli monitoringu ta výjimka existuje.
(b) XML dokumentace jde do IntelliSense i do NuGet balíčku, takže se nepravda šíří k zákazníkům.

**Oprava:**

1. `TaskBase.cs:82–88` — v `catch (DataverseValidationException)` nastavit
   `LogSeverity.Warning` místo `Info`.
2. `docs/plugins/task-model.md:271–274` a `docs/plugins/execution-pipeline.md` — sjednotit
   na `Warning`.
3. `docs/plugins/logging.md` — ověřit dopad na tabulku `MinimalSeverityLevel`: při doporučené
   produkční hodnotě `3` (`Warning`, `Error`) se business zamítnutí **začne v produkci logovat**,
   zatímco dnes se jako `Info` zahazovalo. To je žádoucí (business zamítnutí je informace pro
   support), ale je to **změna objemu produkčních logů** a patří ji zmínit v poznámkách k vydání.
4. Komentář v `examples/…/ValidateNames.cs:33` — už je správný, ponechat.
5. Test, který severitu zafixuje pro oba vstupy: přímý `throw` v `DoExecute()` i
   `ThrowWithWarning(...)` ve validačním řetězu.

> [!WARNING]
> Jde o **změnu chování**, ne o opravu dokumentace: `CHANGELOG.md` + posouzení podle
> `docs/versioning.md`. Kdokoli dnes filtruje logy na `Info`, přestane tyto záznamy vidět tam
> a začne je vidět v `Warning`.

**Otevřená návazná otázka — `TaskStatus` (D3b, §12).** Rozhodnutí D3 řeší severitu, ne stav.
Ten je dnes `Success` pro oba případy, což u `ThrowWithWarning(...)` neodpovídá logice:
výjimka je vyhozena už během `Validate(...)`, tělo tasku se nikdy nespustí, a přesto je
výsledek `Success`. Podle stejného principu („musí odpovídat funkčně i logicky“) by tento
případ měl být `NotValid`, zatímco `throw` z `DoExecute()` `Success` zůstat může.
Implementačně je to oddělení `try` bloku kolem `Validate(...)` od bloku kolem
`ExecuteInternal(...)`. Nedělám to bez rozhodnutí, protože stav se propisuje do statistik
model-driven aplikace a do vyhodnocování „task se často spouští naprázdno“.

**Hotovo když:** kód, obě dokumentace i XML doc tvrdí `Warning`, test to fixuje a změna je
v `CHANGELOG.md`.

---

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

### F2-01 · Název atributu — tři formy · **M** · nejdůležitější položka F2

| Forma | Kde | Verdikt |
|---|---|---|
| `Contact.Fields.FirstName` | `plugin-registration-api.md` | ✅ **kanonická** |
| `"firstname"` | `examples/…/Plugins/ContactPlugin.cs`, `TaskPlugin.cs` | fallback bez early-bound typu |
| `nameof(ContextEntity.FirstName)`, `nameof(…).ToLower()` | `examples/…/ValidateNames.cs`, `UpdateAddressLabel.cs` | ❌ zakázat |

**Proč `Fields` konstanty:** jsou generované z metadat, takže jsou správné konstrukcí.
Ověřeno, že existují a jsou zapnuté defaultně
(`examples/…/EarlyBound/Entities/contact.cs:438,593`, `emitFieldsClasses: true`
v `EarlyBoundSettings.template.json:15`).

**Proč zakázat `nameof(...)`:** dnes to funguje jen náhodou. `EntityAttributesValidator`
(`src/…/Validators/EntityAttributesValidator.cs:8,17`) obě strany převádí na lowercase, a
`UpdateAddressLabel` si volá `.ToLowerInvariant()` ručně. Vazba property → logical name ale
není zaručená (`ActivityId`, enum properties, aliasy) a `.ToLower()` navíc jde proti vlastní
analyzer politice z `CONTRIBUTING.md`. Tichá chyba, která se projeví až za běhu tím, že
se task nespustí.

**Oprava:** kanonizovat `Fields` konstanty, přepsat oba vzorové tasky, doplnit pravidlo
včetně závislosti *`emitFieldsClasses` musí zůstat `true`* a fallbacku pro entity bez
early-bound typu.

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
| F5-01 | Pojmenování stepů (`WithName`) | `"Pillaro Examples PreVal Create Contact"` → vzor `{Prefix} {Stage} {Message} {Entity}` | S |
| F5-02 | Pojmenování tasků a testů | tasky imperativně (`ValidateNames`, `UpdateAddressLabel`, `SummarySync`); testy `Method_Condition_Expectation` | S |
| F5-03 | Kdy `Features/` a kdy privátní metoda | `CustomerForbiddenNameService` je jediný vzor | S |
| F5-04 | Ownership souborů — čeho se AI nesmí dotknout | `EarlyBound/**`, `Tools/**` (přegenerovává balíček), `key.snk`, `appsettings*.json`, `PillaroSettings.json`, `power-platform-solutions/**` (binární zipy) | S |

F5-04 je z nich nejdůležitější: `docs/plugins/early-bound-generation.md` má tabulku
*File Ownership*, ale pokrývá jen early-bound tooling. Pro agenta je potřeba úplný seznam —
generované soubory přepsané modelem se ztratí při dalším buildu a diagnostika toho je drahá.

---

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
| 1 | F1 (všech 9 položek; F1-02 a F1-04 včetně změny kódu a šablony) | 1–1,5 dne | Dokumentace i kód tvrdí totéž |
| 2 | F2-01 … F2-06 + srovnání `/examples` | 1–1,5 dne | Jediná kanonická forma pro každý úkon |
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
| F1-02 mění chování logování (D3) | Kdo filtruje na `Info`, přestane záznamy vidět; v produkci naopak přibudou | `CHANGELOG.md`, posouzení verze, test fixující severitu, poznámka o objemu produkčních logů |
| F1-02 zůstane nedotažené bez D3b | Severita bude `Warning`, ale stav dál `Success` i tam, kde tělo tasku neproběhlo | Rozhodnout D3b spolu s D3 a opravit jednou změnou |
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
| **D1** | Kde leží early-bound klasy | **`Logic`.** V šabloně a příkladech nejsou commitnuté, protože závisí na prostředí — generují se toolingem. | F1-04 přepsáno; přidán návrh vypnout scaffolding v `Plugins` |
| **D3** | Severita u `DataverseValidationException` | **`Warning`.** Chování musí odpovídat funkčně i logicky, takže se opravuje kód, ne dokumentace. | F1-02 z opravy textu na změnu kódu (S → M), + `CHANGELOG.md` a dopad na produkční objem logů |

### Zbývá rozhodnout

| # | Rozhodnutí | Doporučení | Blokuje |
|---|---|---|---|
| **D2** | Kanonická forma názvu atributu | **`Contact.Fields.X`**, `nameof(...)` zakázat, string jen jako fallback | F2-01, F2-06 |
| **D3b** | `TaskStatus` u `DataverseValidationException` — dnes `Success` pro oba případy | **`NotValid`** pro `ThrowWithWarning(...)` (vyhozeno ve `Validate`, tělo tasku neproběhlo), **`Success`** pro `throw` z `DoExecute()`. Plyne ze stejného principu jako D3, ale mění statistiky v model-driven aplikaci. | F1-02 |
| **D4** | Konvence pojmenování stepů — máme navrhnout, nebo existuje dohoda? | Navrhneme `{Prefix} {Stage} {Message} {Entity}` podle `/examples` | F5-01 |
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
