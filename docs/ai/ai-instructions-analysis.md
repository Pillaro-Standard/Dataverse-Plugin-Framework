# Analýza: jak mají vypadat AI instrukce pro vývoj s Pillaro Dataverse Plugin Framework

> [!NOTE]
> Toto je **analytický (pracovní) dokument**, ne hotová sada instrukcí.
> Cílem je popsat, jaké instrukce pro AI nástroje má repozitář (a šablona pro zákaznická
> řešení) obsahovat, aby programátoři mohli reálně vyvíjet s pomocí AI — a co k tomu ještě chybí.
>
> Vychází z auditu celé dokumentace v `/docs`, root `README.md`, `docs/CONTRIBUTING.md`,
> vzorové implementace v `/examples`, šablony v `/templates` a CI pipeline v rootu repozitáře.

---

## 📑 Obsah

- [1. Shrnutí (TL;DR)](#1-shrnutí-tldr)
- [2. Výchozí stav — co audit zjistil](#2-výchozí-stav--co-audit-zjistil)
- [3. Pro koho instrukce píšeme (dvě různé cílovky)](#3-pro-koho-instrukce-píšeme-dvě-různé-cílovky)
- [4. Cílová architektura instrukční sady](#4-cílová-architektura-instrukční-sady)
- [5. Obsah instrukcí — katalog pravidel](#5-obsah-instrukcí--katalog-pravidel)
- [6. Tvrdé hranice AI (human-in-the-loop)](#6-tvrdé-hranice-ai-human-in-the-loop)
- [7. Feedback loop — bez něj AI instrukce nefungují](#7-feedback-loop--bez-něj-ai-instrukce-nefungují)
- [8. Pipeline analýza → task (hlavní konkurenční výhoda)](#8-pipeline-analýza--task-hlavní-konkurenční-výhoda)
- [9. Hygiena dokumentace pro AI (konkrétní nálezy k opravě)](#9-hygiena-dokumentace-pro-ai-konkrétní-nálezy-k-opravě)
- [10. Anti-patterny, které musí instrukce explicitně zakázat](#10-anti-patterny-které-musí-instrukce-explicitně-zakázat)
- [11. Jak měřit, že instrukce fungují](#11-jak-měřit-že-instrukce-fungují)
- [12. Návrh postupu (fáze P0–P3)](#12-návrh-postupu-fáze-p0p3)
- [13. Otevřené otázky a chybějící vstupy](#13-otevřené-otázky-a-chybějící-vstupy)

---

## 1. Shrnutí (TL;DR)

1. **Framework je na AI-asistovaný vývoj architektonicky připravený nadstandardně dobře.**
   Task = jedna izolovaná jednotka s explicitní validací, deterministickým výstupem a vlastním
   logem. To je přesně ta granularita, se kterou LLM pracuje spolehlivě: malý soubor, jasný
   kontrakt, ověřitelný výsledek.

2. **Zároveň v repozitáři neexistuje ani jeden artefakt pro AI nástroje.**
   Žádný `AGENTS.md`, `CLAUDE.md`, `.github/copilot-instructions.md`, `.cursor/rules`,
   žádné skills/prompty, žádný strojově čitelný katalog pravidel. Tvrzení
   *„AI-ready standard“* a *„AI Feedback Loop“* v `README.md` (řádky 9, 360–398) je dnes
   **deklarace záměru, ne implementace**. To je hlavní zjištění analýzy.

3. **Instrukce nemají být jeden velký soubor.** Doporučená struktura je třívrstvá:
   tenký vstupní bod pro každý AI nástroj → jeden **normativní katalog pravidel** s ID
   (`docs/ai/rules/*.md`) → **kanonické code templates**, které AI kopíruje.
   Prózová dokumentace v `/docs` zůstává pro lidi; AI potřebuje krátká imperativní pravidla.

4. **Bez ověřovací smyčky jsou instrukce jen zbožné přání.** Dnes jde všechno ověřit
   pouze proti živému Dataverse na Windows (viz `PR – Validate.yml`). AI potřebuje
   **offline gate**, který zvládne sama: build se zero-warning politikou + validace
   registračního manifestu bez připojení k Dataverse.

5. **Největší obchodní hodnota není v tomto repozitáři, ale v zákaznických řešeních.**
   Instrukční sada proto musí být součástí `dotnet new` šablony a generovaného tooling
   v NuGet balíčku (`Tools/`), jinak ji dostane jen tým frameworku.

> [!NOTE]
> Prováděcí plán vyplývající z této analýzy je v samostatném dokumentu:
> **[Plán oprav a zlepšení před zavedením AI instrukcí](./ai-readiness-fix-plan.md)**.
> Obsahuje ověřené nálezy včetně dvou rozporů dokumentace vs. kód, které analýza
> ještě neobsahovala (nekompilovatelný příklad ve `validation.md` a nekonzistentní popis
> chování `DataverseValidationException`).

### 1.1 Potvrzená rozhodnutí zadavatele

| # | Otázka | Rozhodnutí |
|---|---|---|
| Q1 | Cílovka instrukcí | **Vývojáři zákaznických řešení** (výstup šablony). Instrukce musí být distribuovány v `dotnet new` šabloně a v generovaném tooling NuGet balíčku. |
| Q2 | Podporované AI nástroje | **Všechny přes `AGENTS.md`** jako kanonický vstup; ostatní soubory jsou tenké redirecty. |
| Q3 | GUIDy stepů a images | **Generuje tooling.** AI GUIDy nikdy nepíše ani neopisuje. Vyžaduje dodělat CLI příkaz — je to prerekvizita, ne volitelné vylepšení. |
| Q4 | Ověřovací prostředí | **Dedikované Dataverse dev prostředí je k dispozici**, agent smí pouštět integrační testy. Plná smyčka *log → oprava → test* je tedy reálná. |

Tato rozhodnutí jsou už zapracovaná do §3, §6.1, §7 a §12 níže.

Další rozhodnutí (**D1** — umístění early-bound klas, **D3** — severita
`DataverseValidationException`) jsou vedená v
[plánu oprav](./ai-readiness-fix-plan.md#rozhodnuto), aby existoval jediný seznam.
Do katalogu pravidel v §5 jsou už promítnutá.

---

## 2. Výchozí stav — co audit zjistil

### 2.1 Co je silné

| Oblast | Proč to pomáhá AI |
|---|---|
| Task-based model (`docs/plugins/task-model.md`) | Jedna zodpovědnost = malý generovatelný soubor s ověřitelným kontraktem |
| Oddělení `AddValidations()` / `DoExecute()` | AI nemíchá podmínky a logiku; review je triviální |
| **Fixní pořadí fluent validace** (`docs/plugins/validation.md`) | Deterministické, vynutitelné, IntelliSense vede k jedinému správnému tvaru |
| Připravený runtime surface v `TaskBase<TEntity>` | AI nemusí (a nesmí) bootstrapovat služby → méně halucinací |
| Registrace v kódu (`Register(IPluginRegistration)`) | Deployment metadata jsou text v repu → generovatelná i reviewovatelná |
| Validátor manifestu (`docs/plugins/plugin-registration-api.md`, sekce Validation Rules) | Hotový strojový gate, který lze pustit bez Dataverse |
| Task-level logy se stavy `Success` / `NotValid` / `Error` | Strukturovaný signál zpět do AI (základ feedback loopu) |
| Zero-warning quality gate + výčet analyzer kódů (`docs/CONTRIBUTING.md`) | Přímo přeložitelné na tvrdá pravidla |
| Explicitní bezpečnostní kontexty (`User` / `InitiatingUser` / `Admin`) | Lze zakázat eskalaci práv jako default |

### 2.2 Co chybí

| Chybí | Důsledek pro AI vývoj |
|---|---|
| Jakýkoli instrukční soubor pro AI nástroj | Každý vývojář promptuje po svém, výstupy nejsou konzistentní |
| Normativní pravidla (MUST / MUST NOT) | Dokumentace je popisná („recommendations“), model si vybere, co se mu hodí |
| Jediný kanonický příklad na koncept | V repu jsou **tři různé** styly práce s názvy atributů (viz §9.2) → AI kopíruje ten první, který uvidí |
| Offline ověření | AI nemá jak zjistit, že vygenerovala nesmysl, dokud to nepustí člověk na Windows proti živému prostředí |
| Politika pro GUIDy stepů/images | Validátor placeholdery odmítá, AI je přesto bude vymýšlet |
| Konvence pojmenování stepů (`WithName(...)`) | Nedokumentováno → AI si vymyslí vlastní názvy, deployment vytvoří nekonzistentní metadata |
| Strojově čitelný formát vstupu (analýza → task) | „Tasky lze identifikovat už v analýze“ (`README.md`) není nijak formalizováno |
| Instrukce v šabloně/NuGet balíčku | Zákaznické projekty by je nedostaly |

### 2.3 Stav ověřovací infrastruktury (důležité pro návrh)

- CI je **Azure DevOps**, `windows-latest`, buildy přes `VSBuild` (`PR – Validate.yml`,
  `Nightly – Tests Only.yml`, `Packages – Build & Package.yml`).
- Testy dostávají `ConnectionStrings__Dataverse` ze secret groupy → **integrační testy
  vyžadují živé prostředí**; podle `docs/tests/testing.md` je to záměr („do not mock Dataverse services“).
- V `tests/` ale **už existují offline testy** (`PluginRegistrationDiscoveryTests`,
  `DeploymentScaffoldingTests`) — částečná offline smyčka tedy jde postavit, není to greenfield.
- Deployment i ILMerge jsou Windows-only (`Tools/ILMerge`, `key.snk`, `DeployPlugins.bat/.ps1`).

---

## 3. Pro koho instrukce píšeme (dvě různé cílovky)

Tohle je nejdůležitější rozhodnutí před psaním jediného řádku, protože pravidla se
z ~30 % rozcházejí.

### A) Contributor frameworku (tento repozitář)
Pracuje v `src/`, `tools/`, `tests/`, `templates/`. Potřebuje: pravidla veřejného API,
zpětnou kompatibilitu (`docs/versioning.md`), zero-warning gate, `CHANGELOG.md`,
PR proti `develop`, zákaz breaking changes bez major verze.

### B) Vývojář zákaznického řešení (výstup šablony)
Pracuje v `YourSolution.Logic` / `.Plugins` / `.Tests`. Potřebuje: kam patří jaký soubor,
jak napsat task + validaci + registraci + test, jaké GUIDy použít, jak nasadit.
**Tady je 90 % objemu práce a tedy i hodnoty AI.**

**Rozhodnuto (Q1): cílovka je B).**
Instrukční sada se píše pro vývojáře zákaznických řešení a **musí se distribuovat
v `dotnet new` šabloně a v generovaném tooling NuGet balíčku** — jinak ji dostane jen
tým frameworku. To má tři praktické důsledky pro celý návrh:

1. **Zdroj pravdy je v tomto repozitáři, cíl je jinde.** Pravidla se udržují zde
   (`docs/ai/rules/`) a *publikují* do šablony a do balíčku stejným mechanismem jako
   dnešní `Tools/Deployment` a `Tools/EarlyBound`. Ruční kopírování povede k driftu.
2. **Pravidla nesmí předpokládat kontext frameworkového repozitáře.** Žádné odkazy na
   `src/`, `tools/`, `CHANGELOG.md` ani PR proti `develop` — v zákaznickém řešení nic
   z toho neexistuje. Odkazy do dokumentace musí být absolutní URL na GitHub, ne
   relativní cesty.
3. **Contributor pravidla (A) se tím neruší, jen klesají v prioritě.** Vzniknou později
   jako samostatná, tenčí sada nad stejným jádrem (§12, fáze P3).

---

## 4. Cílová architektura instrukční sady

### 4.1 Vrstvy

~~~text
L0  Vstupní body pro nástroje (tenké, jen odkazy)
    AGENTS.md                      ← kanonický vstup
    CLAUDE.md                      ← 5 řádků: "read AGENTS.md"
    .github/copilot-instructions.md← 5 řádků: "read AGENTS.md"
    .cursor/rules/pillaro.mdc      ← 5 řádků: "read AGENTS.md"

L1  Normativní pravidla (jediný zdroj pravdy pro AI)
    docs/ai/rules/00-architecture.md
    docs/ai/rules/10-plugin.md
    docs/ai/rules/20-task.md
    docs/ai/rules/30-validation.md
    docs/ai/rules/40-data-access.md
    docs/ai/rules/50-logging-errors.md
    docs/ai/rules/60-registration.md
    docs/ai/rules/70-testing.md
    docs/ai/rules/80-build-quality.md
    docs/ai/rules/90-process-security.md

L2  Kanonické šablony kódu (to, co AI kopíruje)
    docs/ai/templates/Task.cs.md
    docs/ai/templates/Plugin.cs.md
    docs/ai/templates/Registration.cs.md
    docs/ai/templates/IntegrationTest.cs.md
    docs/ai/templates/Repository.cs.md

L3  Opakovatelné workflow (skills / slash commands / prompty)
    .claude/skills/new-task/…      "vytvoř task ze zadání"
    .claude/skills/new-step/…      "přidej plugin step + zarovnej RegisterTask"
    .claude/skills/new-test/…      "dopiš integrační test k tasku"
    .claude/skills/review-pf/…     "zkontroluj diff proti katalogu pravidel"

L4  Ověření (to, co AI spouští sama)
    docs/ai/verify.md              ← přesné příkazy fast/slow loopu
    (nový) pillaro validate        ← offline validace manifestu, viz §7.2

L5  Distribuce do zákaznických řešení
    templates/…/ProjectTemplate/AGENTS.md
    src/…/Tools/AI/                ← generovaný instrukční pack v NuGet balíčku
~~~

### 4.2 Proč `AGENTS.md` jako kanonický vstup

`AGENTS.md` dnes čte většina relevantních nástrojů (Copilot coding agent, Codex, Cursor,
Claude Code, Jules). Ostatní soubory jsou jen tenké redirecty — tím se eliminuje
**drift**, což je nejčastější příčina toho, že instrukce po pár měsících lžou.

### 4.3 Tvarová pravidla pro samotné instrukce

- **Rozsah:** `AGENTS.md` do ~150 řádků. Vše delší se nečte celé a soutěží o kontext.
- **Tón:** imperativ. `MUST`, `MUST NOT`, `NEVER`, `ALWAYS`. Ne „recommended“, ne „usually“.
- **ID pravidel:** každé pravidlo dostane stabilní ID (`PF-TASK-004`), aby na něj mohl
  odkazovat review, eval i commit message.
- **Struktura pravidla:** *pravidlo → proč (1 věta) → správně (kód) → špatně (kód)*.
  Negativní příklad je u LLM stejně důležitý jako pozitivní.
- **Žádná duplikace prózy z `/docs`.** Pravidlo + odkaz. Duplikát se rozejde.
- **Jazyk:** angličtina (kód, API, dokumentace i publikované balíčky jsou EN).
  Konverzace s vývojářem může být česky — to patří do preferencí, ne do repo pravidel.
- **Uveď i „co NEdělat“ na úrovni scope:** čeho se AI nesmí dotknout (viz §6).

---

## 5. Obsah instrukcí — katalog pravidel

Návrh konkrétních pravidel odvozených 1:1 z existující dokumentace. Sloupec *Zdroj*
je důvod, proč pravidlo v katalogu je — a zároveň důkaz, že si nic nevymýšlíme.

### 5.1 Architektura a umístění kódu

| ID | Pravidlo | Zdroj |
|---|---|---|
| PF-ARCH-001 | Veškerá business logika MUSÍ být v projektu `Logic`. | architecture.md |
| PF-ARCH-002 | Projekt `Plugins` je pouze deployment shell — NIKDY tam nepatří task, feature ani doménová logika. | architecture.md, getting-started.md |
| PF-ARCH-003 | Testy referencují `Logic`, NIKDY mergovaný `Plugins` výstup. | limitations.md |
| PF-ARCH-004 | Struktura: `Plugins/` (orchestrace), `Tasks/` (business logika), `Features/` (znovupoužitelné služby). | getting-started.md §3.3 |
| PF-ARCH-005 | Struktura `Tests/` zrcadlí strukturu `Tasks/`. | testing.md |
| PF-ARCH-006 | Nové soubory se zakládají do existující struktury; nová top-level složka vyžaduje souhlas člověka. | — (odvozeno) |

### 5.2 Plugin

| ID | Pravidlo | Zdroj |
|---|---|---|
| PF-PLUG-001 | Plugin dědí ze *solution* `PluginBase`, ne přímo z frameworkového. | getting-started.md §7 |
| PF-PLUG-002 | Konstruktor obsahuje pouze `RegisterTask<T>(...)` volání. Žádné podmínky, žádné dotazy do Dataverse. | plugin-model.md |
| PF-PLUG-003 | Deployment metadata pouze v `Register(IPluginRegistration)`. | plugin-registration-api.md |
| PF-PLUG-004 | Pojmenování: entitně orientovaně (`ContactPlugin`) nebo dle business capability. | plugin-model.md |

### 5.3 Task

| ID | Pravidlo | Zdroj |
|---|---|---|
| PF-TASK-001 | Task dědí z `TaskBase<TEntity>` s early-bound typem. | task-model.md |
| PF-TASK-002 | Jeden task = jedna business zodpovědnost. Dvě nesouvisející operace = dva tasky. | task-model.md |
| PF-TASK-003 | Podmínky POUZE v `AddValidations()`, logika POUZE v `DoExecute()`. Žádné „guard“ ify na začátku `DoExecute()`, které patří do validace. | validation.md |
| PF-TASK-004 | NIKDY nebootstrapovat `IOrganizationService` ručně — použij připravené providery. | data-access.md |
| PF-TASK-005 | Sdílený `TaskContext` používat vědomě; ne jako skrytý mechanismus závislostí mezi tasky. | execution-pipeline.md |
| PF-TASK-006 | Znovupoužitelnou logiku vytáhnout do `Features/`, ne kopírovat mezi tasky. | task-model.md |

### 5.4 Validace (nejcennější vynutitelné pravidlo)

| ID | Pravidlo | Zdroj |
|---|---|---|
| PF-VAL-001 | Pořadí řetězu je FIXNÍ: `WithMode` → `WithStage` → `WithMessage(s)` → `ForEntity(-ies)` → image checks → attribute checks → `WithValidation` → `WithBreakValidation` / `ThrowWith*`. | validation.md |
| PF-VAL-002 | `WithValidation(...)` jen pro kontroly bez dotazu do Dataverse. | validation.md |
| PF-VAL-003 | Cokoli, co čte z Dataverse, MUSÍ být v `WithBreakValidation(...)` a až na konci řetězu. | validation.md |
| PF-VAL-004 | Žádný „god predicate“ — místo jednoho velkého lambda výrazu více pojmenovaných validací. | validation.md |
| PF-VAL-005 | Každá validace má lidsky čitelnou zprávu (jde do logu i k uživateli). | validation.md |
| PF-VAL-006 | Filtrování zúžit registrací (filtering attributes) i validací; nespoléhat jen na jedno. | validation.md, execution-pipeline.md |

### 5.5 Data access a bezpečnost

| ID | Pravidlo | Zdroj |
|---|---|---|
| PF-DATA-001 | Default je `DataServiceProvider`; `OrganizationServiceProvider` jen když je potřeba přímý `IOrganizationService`. | data-access.md |
| PF-DATA-002 | Nejnižší dostačující kontext. `Admin` NIKDY jako default — vyžaduje komentář s odůvodněním. | data-access.md |
| PF-DATA-003 | Zvolený kontext musí být v kódu vizuálně zřejmý; nezakrývat ho helperem. | data-access.md |
| PF-DATA-004 | Early-bound typy s prefixem `Logic.` (`Logic.Contact`). | CONTRIBUTING.md |
| PF-DATA-005 | AI NIKDY nepíše ani needituje soubory v `EarlyBound/` — generuje je `pac modelbuilder`. | early-bound-generation.md (File Ownership) |
| PF-DATA-006 | Early-bound klasy leží v projektu `Logic`; generování se spouští z rootu `Logic`. `src/` frameworku není vzor (nemá `Logic` projekt). | rozhodnutí D1, architecture.md |
| PF-DATA-007 | Chybějící early-bound typ nebo atribut = AI zastaví a řekne, co vygenerovat. NIKDY nedopisovat partial class ani obcházet late-bound. | early-bound-generation.md |

### 5.6 Logování a chyby

| ID | Pravidlo | Zdroj |
|---|---|---|
| PF-LOG-001 | Task-level diagnostiku psát přes `AddLogMessageLine(...)` / `AddLogDetail(...)`, ne přes `LogService`. | task-model.md, logging.md |
| PF-LOG-002 | Nelogovat vstupní parametry a images — framework je loguje automaticky. | task-model.md |
| PF-LOG-003 | Do logu NIKDY tajemství, tokeny ani osobní údaje nad rámec business potřeby. | SECURITY.md, step-configuration.md |
| PF-ERR-001 | Očekávané business zastavení = `DataverseValidationException`: uživatel vidí zprávu, task je `Success` + `Info`. Ne `InvalidPluginExecutionException` — ten vytvoří falešný `Error` v monitoringu. | error-handling.md, execution-pipeline.md, rozhodnutí D3 |
| PF-ERR-002 | `InvalidPluginExecutionException` v task kódu jen výjimečně — framework převod řeší sám. | error-handling.md |
| PF-ERR-003 | Nebudovat vlastní try/catch pipeline v tasku. | error-handling.md |

> [!NOTE]
> `PF-ERR-001` odpovídá skutečnému chování v `TaskBase.cs:82–88` (rozhodnutí **D3**).
> „Warning“ v `ThrowWithWarning(...)` označuje povahu hlášky pro uživatele, ne úroveň logu —
> task splnil svou práci, takže `Success`, a záznam je informativní, takže `Info`.
> Dokumentace, která tvrdila něco jiného, je opravená v položce
> [F1-02 plánu oprav](./ai-readiness-fix-plan.md#f1-02--dokumentace-i-xml-doc-popisují-dataversevalidationexception-jinak-než-kód--s).

### 5.7 Registrační metadata (nejrizikovější oblast pro AI)

| ID | Pravidlo | Zdroj |
|---|---|---|
| PF-REG-001 | `RegisterTask(...)` a `Register(...)` MUSÍ zůstat zarovnané (stage, message, entita, mode). Změna jednoho = kontrola druhého. | plugin-registration-api.md |
| PF-REG-002 | Step ID a image ID MUSÍ být neprázdné GUIDy; `Guid.Empty` a placeholder vzory validátor odmítá. | plugin-registration-api.md |
| PF-REG-003 | AI NIKDY nevymýšlí GUIDy bez explicitní politiky (viz §6.1 a otázka Q3). | — (odvozeno) |
| PF-REG-004 | Synchronní Update step MUSÍ mít filtering attributes; preferovat `WhenChanged(...)`. | plugin-registration-api.md |
| PF-REG-005 | Create step nesmí mít pre-image; Delete step nesmí mít post-image; images jen v Pre/PostOperation. | plugin-registration-api.md |
| PF-REG-006 | Názvy images unikátní v rámci stepu, image ID unikátní v rámci manifestu. | plugin-registration-api.md |
| PF-REG-007 | Preferovat typovaný výběr atributů (`c => c.FirstName`) nebo konstanty (`Contact.Fields.FirstName`); string literály jen když early-bound typ neexistuje. | plugin-registration-api.md |
| PF-REG-008 | `WithName(...)` podle dohodnuté konvence pojmenování stepů. | **konvence dnes neexistuje — nutno doplnit (Q)** |

### 5.8 Testy

| ID | Pravidlo | Zdroj |
|---|---|---|
| PF-TEST-001 | Testy jsou integrační proti živému Dataverse. NIKDY nemockovat Dataverse služby. | testing.md |
| PF-TEST-002 | Testovací záznamy zakládat `TestDataService.CreateTestEntity(...)`, ne `OrganizationService.Create(...)`. | testing.md |
| PF-TEST-003 | Data pro testy z repository v `Data/Repositories/` (`IAutoRegisteredTestDataRepository`). | testing.md |
| PF-TEST-004 | Každá test class má `[Trait("Owner", …)]` a `[Trait("Category", nameof(SomeTask))]`. | testing.md |
| PF-TEST-005 | Ke každému novému tasku vzniká alespoň jeden test na happy path a jeden na business zamítnutí. | getting-started.md (Recommendations) |
| PF-TEST-006 | Test kód bez warningů. | testing.md |
| PF-TEST-007 | AI NIKDY nespouští integrační testy proti prostředí bez explicitního pokynu. | — (odvozeno) |

### 5.9 Build quality gate

| ID | Pravidlo | Zdroj |
|---|---|---|
| PF-BUILD-001 | Nula warningů z kompilátoru i analyzátorů. | CONTRIBUTING.md |
| PF-BUILD-002 | Zakázané vzory: `SYSLIB1045` (žádný `[GeneratedRegex]` v sandboxu), `CA1862`, `CA1861`, `CA1822`, `IDE0005`, `IDE0028`. | CONTRIBUTING.md |
| PF-BUILD-003 | Plugin projekty `net462`, `<LangVersion>latest</LangVersion>`. | getting-started.md |
| PF-BUILD-004 | Výstup musí zůstat ILMerge kompatibilní; nepřidávat závislosti neplatné v sandboxu. | CONTRIBUTING.md, architecture.md |
| PF-BUILD-005 | Warning NIKDY nepotlačovat přes `#pragma` / `NoWarn` jako řešení — opravit příčinu. | — (odvozeno) |

### 5.10 Proces a bezpečnost

| ID | Pravidlo | Zdroj |
|---|---|---|
| PF-PROC-001 | PR míří na `develop`, nikdy na `main`. Branch naming `feature/` \| `bugfix/` \| `docs/`. | CONTRIBUTING.md |
| PF-PROC-002 | Změna chování = záznam v `CHANGELOG.md`. | CHANGELOG.md, versioning.md |
| PF-PROC-003 | Změna veřejného API = kontrola dopadu na verzování. | versioning.md |
| PF-PROC-004 | NIKDY necommitovat connection string, secret ani `key.snk` obsah; lokálně user-secrets. | testing.md, SECURITY.md |
| PF-PROC-005 | AI nespouští deployment do Dataverse. | deployment-plugins.md |
| PF-PROC-006 | Zranitelnosti neřešit ve veřejném issue. | SECURITY.md |

---

## 6. Tvrdé hranice AI (human-in-the-loop)

Sekce, která v praxi rozhoduje o tom, jestli je AI použitelná, nebo jestli tiše rozbíjí
produkční prostředí. Instrukce ji musí obsahovat explicitně a nahoře.

### 6.1 GUIDy plugin stepů a images — **rozhodnuto: generuje tooling (Q3)**

Validátor odmítá `Guid.Empty` i placeholder vzory a model si GUID vymyslí vždy, když mu
to nezakážeme. Zvolená politika je proto **generování toolingem**: AI GUID nikdy nepíše.

Co z toho plyne:

- **Prerekvizita:** musí vzniknout příkaz, který step/image zaregistruje a ID doplní sám
  (např. `pillaro new-step --plugin ContactPlugin --message Update --stage PreOperation`).
  Bez něj politika neexistuje a AI se vrátí k vymýšlení. Proto je v §12 zařazen do P1,
  ne mezi „nice to have“.
- **Pravidlo pro AI:** při přidání stepu AI *volá tooling* a do kódu doplní ID, které
  tooling vrátil. Nikdy negeneruje vlastní GUID, nikdy needituje existující ID.
- **Tvrdý zákaz:** AI NIKDY nekopíruje GUIDy z dokumentace ani z `/examples`
  (`4e56ef4c-0e08-f111-8407-000d3ab261ac` apod.). Kolize ID napříč zákaznickými řešeními
  je nejhorší možný výsledek — deployment by přepisoval cizí registrační metadata.
- **Pojistka:** offline validace manifestu (§7.2/1) musí kromě placeholderů odmítnout
  i známé GUIDy z dokumentace a příkladů. To udělá z pravidla vynutitelný gate.

### 6.2 Early-bound klasy
Generuje `pac modelbuilder` proti živému prostředí. AI je NIKDY nepíše ani needituje.
Když potřebný typ/atribut chybí, AI **zastaví a řekne, co má člověk vygenerovat** —
nesmí si dopsat vlastní partial class ani použít late-bound obcházku.

### 6.3 Ověření business chování
Integrační testy potřebují prostředí + credentials + nasazené pluginy. AI si tedy
**nemůže sama potvrdit, že task funguje**. Instrukce musí být poctivé: AI garantuje
strukturu, pravidla a build; funkční potvrzení dělá člověk nebo CI.

### 6.4 Deployment, ILMerge, signing
Windows-only, `key.snk`, připojení do Dataverse. AI připravuje a kontroluje metadata,
**nespouští** deployment.

### 6.5 Import frameworkové solution
Předpoklad běhu (settings, logging). AI to jen ověřuje jako předpoklad v checklistu.

### 6.6 Produkční nastavení
`MinimalSeverityLevel` v produkci `3` (výchozí doporučení). AI nesmí navrhovat plné
logování v produkčním prostředí jako řešení diagnostiky bez upozornění na dopad.

---

## 7. Feedback loop — bez něj AI instrukce nefungují

Instrukce určují, co AI napíše. **Smyčka určuje, jestli to bude fungovat.**
Model bez rychlého ověření se nemá jak opravit.

### 7.1 Dvě smyčky

| | Fast loop (AI si pouští sama) | Slow loop (člověk / CI) |
|---|---|---|
| Co | build + analyzátory + validace manifestu + offline unit testy | integrační testy proti Dataverse |
| Délka | sekundy až desítky sekund | minuty, potřebuje prostředí |
| Kdo spouští | agent | **agent (dedikované dev prostředí, Q4)**, vývojář nebo pipeline |
| Dnešní stav | **částečně chybí** | existuje (`PR – Validate.yml`, nightly) |

**Rozhodnuto (Q4):** agent má k dispozici dedikované Dataverse dev prostředí, takže smí
pouštět i slow loop. Tím se ale mění charakter rizika — agent teď může zapisovat do
živého prostředí. Instrukce proto musí obsahovat tvrdá pravidla pro práci s prostředím:

| ID | Pravidlo |
|---|---|
| PF-ENV-001 | Agent pracuje POUZE proti dedikovanému dev prostředí. NIKDY proti test/UAT/produkci. |
| PF-ENV-002 | Connection string se čte z user-secrets nebo env proměnné. NIKDY se nepíše do souboru v repu ani do logu/výstupu. |
| PF-ENV-003 | Zápis dat POUZE přes `TestDataService.CreateTestEntity(...)`, aby cleanup zafungoval (PF-TEST-002). |
| PF-ENV-004 | Agent NIKDY nemaže ani neupravuje záznamy, které nevytvořil v aktuálním běhu. |
| PF-ENV-005 | Agent nespouští deployment ani neregistruje assembly (PF-PROC-005) — testuje proti tomu, co je nasazené. |
| PF-ENV-006 | Před spuštěním integračních testů agent ověří, že cílové prostředí je to dev (kontrola URL proti očekávané hodnotě), a při neshodě zastaví. |
| PF-ENV-007 | Selhání integračního testu se NIKDY „neopraví“ úpravou testu tak, aby prošel — opravuje se task, nebo se nález eskaluje člověku. |

### 7.2 Co je potřeba dodělat (konkrétní návrhy)

1. **Offline validace registračního manifestu — už je naprogramovaná, jen není dosažitelná.**
   V `Pillaro.Dataverse.PluginFramework.Cli` existují hotové příkazy `PluginManifestCommand`
   (assembly → manifest JSON, **bez připojení**) a `PluginValidateCommand`
   (manifest → validace, **bez připojení**), oba s nenulovými exit kódy. Router
   (`PluginCommandRouter.cs:13–20`) ale routuje **pouze `deploy`** — `manifest`, `validate`
   i `diff` jsou nedosažitelný kód a nejsou v helpu.
   Zprovoznění je tedy otázka několika řádků, ne nové funkcionality, a dává rovnou
   deterministický gate na nejrizikovější oblast (§5.7).
   Detail a postup: [F3-01 v plánu oprav](./ai-readiness-fix-plan.md#f3-01--zprovoznit-validate-a-manifest-v-cli-routeru--s--nejlepší-poměr-cenapřínos).
2. **`docs/ai/verify.md` s doslovnými příkazy.** Nikdy „spusť build“, vždy konkrétní
   příkazová řádka pro `Logic`, pro test projekty a pro validaci — včetně toho,
   co dělat při jednotlivých typech selhání.
3. **Warnings jako chyba pro AI běh.** `-warnaserror` (nebo `TreatWarningsAsErrors`
   v CI/AI profilu) proměňuje PF-BUILD-001 z proklamace na vynucené pravidlo.
4. **Diagnostic Log → vstup pro AI.** Framework loguje strukturovaně
   (task, status, severity, elapsed, correlation id, detaily). Chybí **cesta, jak se
   log dostane k agentovi**: export view / FetchXML dotaz do souboru, nebo MCP server
   nad Dataverse. Tím se z marketingového „AI Feedback Loop“ stane reálná smyčka:
   *log selhání → prompt → oprava tasku → test*.
   Protože dev prostředí je podle Q4 dostupné, **není to už blokované na infrastruktuře**
   — jde jen o dodělání čtecí cesty. Minimální varianta je jednoduchý dotaz na
   `Diagnostic Log` filtrovaný podle `correlation id` posledního testovacího běhu,
   uložený do souboru, který agent přečte. To je nejrychlejší způsob, jak smyčku uzavřít;
   MCP server nad Dataverse je pohodlnější, ale výrazně dražší.
5. **Ověření na šablonovém výstupu.** `scripts/Test-DotNetTemplateArtifacts.ps1` už dělá
   smoke build vygenerovaného projektu — je to přirozené místo, kde ověřit, že se
   instrukční pack do zákaznického řešení skutečně dostal.

---

## 8. Pipeline analýza → task (hlavní konkurenční výhoda)

`README.md` tvrdí: *„tasks can be identified already during the analysis phase“* a
*„tasks can be implemented by specialized AI agents based on analysis outputs“*.
Dnes tomu nic neodpovídá. Přitom je to jediná část, kterou konkurence nemá zdarma —
task-based architektura dává **1:1 mapování mezi položkou analýzy a jedním souborem kódu**.

**Návrh: strojově čitelná specifikace tasku.** Vstup pro AI (i pro review s analytikem),
ne jen prompt v chatu:

~~~yaml
task: ValidateContactNames
entity: contact                 # logical name
messages: [Create, Update]
stage: Prevalidation
mode: Synchronous
trigger:
  filteringAttributes: [firstname, lastname]
  requiredImages: []
preconditions:
  - "alespoň jeden z firstname / lastname je v cílové entitě"
rules:
  - id: R1
    description: "firstname nesmí být zakázané slovo z nastavení ForbiddenNames"
    onFailure: userMessage      # → DataverseValidationException
    message: "First name is a forbidden word."
dataAccess:
  context: User                 # User | InitiatingUser | Admin (+ odůvodnění)
  reads: [pl_setting]
logging:
  messageLines: ["seznam zakázaných slov"]
tests:
  - name: CreateContact_WithAllowedName_Succeeds
    expect: success
  - name: CreateContact_WithForbiddenName_Throws
    expect: InvalidPluginExecutionException
~~~

Z tohoto vstupu je AI schopná deterministicky vygenerovat task, validační řetěz,
registrační metadata i skelet testů — a co je důležitější, **review se dělá proti
specifikaci**, ne proti dojmu z kódu. Zároveň to dává formát, ve kterém může analytik
předat práci bez znalosti C#.

Je to zároveň nejdražší část celého návrhu, takže patří do fáze P3 — až po tom,
co P0–P2 fungují.

---

## 9. Hygiena dokumentace pro AI (konkrétní nálezy k opravě)

Modely čtou tuto dokumentaci jako primární kontext. Následující nálezy zhoršují výstup
měřitelně a jsou opravitelné rychle.

### 9.1 Formátování bloků kódu
V `/docs` se souběžně používají **tři styly**: odsazené bloky (bez jazyka), `~~~` a
```` ``` ````. Napříč `/docs` je **49 fenced bloků bez jazykové značky**. Klíčové soubory
(`task-model.md`, `plugin-model.md`, `validation.md`, `getting-started.md`, `logging.md`)
mají příklady jen jako odsazený text.
**Doporučení:** vše na ```` ```csharp ```` / ```` ```json ```` / ```` ```powershell ```` / ```` ```text ````.
Bez jazykové značky model hůř rozlišuje kód od prózy a častěji míchá C# s pseudokódem.

### 9.2 Rozporné kanonické příklady (nejzávažnější nález)
Pro totéž — název atributu — má repozitář **tři různé formy**:

| Forma | Kde |
|---|---|
| `Contact.Fields.FirstName` | `plugin-registration-api.md` (doporučeno) |
| `"firstname"` (string literál) | `examples/…/Plugins/ContactPlugin.cs` |
| `nameof(ContextEntity.FirstName)` / `nameof(…).ToLower()` | `examples/…/Tasks/Contact/ValidateNames.cs`, `UpdateAddressLabel.cs` |

AI kopíruje příklad, který má nejblíž — výstup je proto nekonzistentní **a nemá to nic
společného s kvalitou instrukcí**. Navíc `nameof(...).ToLower()` je fragilní a naráží na
vlastní analyzer politiku z `CONTRIBUTING.md`.
**Doporučení:** zvolit jednu kanonickou formu, sjednotit `/examples` a `/docs`, a v
katalogu pravidel to zafixovat jako PF-REG-007 / PF-DATA-004. Pro AI vývoj má
`/examples` větší váhu než próza — je to spustitelný kód.

> [!NOTE]
> Nálezy z této sekce jsou rozpracované do konkrétních opravných položek
> v [plánu oprav](./ai-readiness-fix-plan.md) (F1 a F4), včetně dalších, které
> vznikly až ověřením příkladů proti zdrojovému kódu.

### 9.3 Nefunkční / nepřesné odkazy a názvy
| Nález | Dopad na AI |
|---|---|
| `docs/CONTRIBUTING.md` uvádí `Pillaro.Dataverse.PluginFramework.sln`; reálný soubor je `Dataverse Plugin Framework.sln` | Agent podle instrukcí selže na prvním kroku |
| `docs/README.md:175` odkazuje `./VERSIONING.md`; soubor je `docs/versioning.md` | Rozbitý odkaz (GitHub je case-sensitive) |
| `getting-started.md` má dvakrát sekci `### 4.3` | Nejednoznačná reference v promptu |
| `plugin-registration-api.md`: „The examples **above** use `Guid.Empty` placeholders“, ale příklad je níž | Model si odvodí špatný kontext varování |
| Root `README.md` slibuje AI-ready, repozitář neobsahuje žádný AI artefakt | Nejde o technický bug, ale o rozpor tvrzení a reality |

### 9.4 Chybějící konvence, které si AI vymyslí
- **Pojmenování plugin stepů** (`WithName(...)`) — v `/examples` `"Pillaro Examples PreVal Create Contact"`, nikde není pravidlo. Bez konvence dostane každé řešení jiný styl.
- **Pojmenování tasků** — z příkladů lze odvodit imperativ (`ValidateNames`, `UpdateAddressLabel`, `SummarySync`), ale není to nikde napsáno.
- **Pojmenování testů** — vzor `Method_Condition_Expectation` se v příkladech drží, chybí explicitně.
- **Kdy `Features/` vs. metoda v tasku** — hranice není definovaná.

---

## 10. Anti-patterny, které musí instrukce explicitně zakázat

Vychází z varování, která si framework sám dává (`> [!WARNING]`, `> [!IMPORTANT]`)
— tedy z reálných chyb, které tým už viděl. LLM je udělá s vysokou pravděpodobností,
protože jsou to nejčastější vzory z veřejného Dataverse kódu na internetu:

1. Business logika v plugin classe místo v tasku.
2. Ruční `serviceProvider.GetService(typeof(IOrganizationServiceFactory))` v tasku.
3. Dotaz do Dataverse ve `WithValidation(...)` (má být `WithBreakValidation`).
4. Guard podmínky na začátku `DoExecute()` místo validačního řetězu.
5. `Admin` kontext jako výchozí volba.
6. `InvalidPluginExecutionException` pro očekávané business zamítnutí (falešné `Error` v monitoringu).
7. Vlastní try/catch/log pipeline v tasku.
8. Mockování `IOrganizationService` v testech.
9. `OrganizationService.Create(...)` v testu (rozbije cleanup).
10. Reference mergované `Plugins` assembly z testů.
11. Potlačení warningu místo opravy.
12. Hand-written „early-bound“ klasy nebo late-bound obcházka chybějícího typu.
13. Zkopírované GUIDy stepů z dokumentace nebo `/examples`.
14. Úprava `RegisterTask(...)` bez zarovnání `Register(...)` (a naopak).

Každý z těchto bodů patří do instrukcí i s negativním příkladem kódu — u LLM funguje
„takhle ne, protože X“ lépe než jen pozitivní vzor.

---

## 11. Jak měřit, že instrukce fungují

Bez měření se instrukční sada po dvou měsících rozejde s realitou. Návrh minimální sady metrik:

| Metrika | Jak měřit | Cíl |
|---|---|---|
| First-pass build rate | podíl AI výstupů, které projdou buildem se zero-warning bez zásahu | > 90 % |
| Registration validity rate | podíl výstupů procházejících offline validací manifestu | 100 % |
| Rule violations / PR | počet nálezů proti katalogu (ID pravidel) v review | klesající trend |
| Human edit distance | rozsah manuálních úprav po AI výstupu | klesající trend |
| Instruction drift | čas mezi změnou frameworku a aktualizací pravidel | < 1 release |

K tomu **golden set 8–12 zadání** (`docs/ai/evals/`) pokrývající: jednoduchý validační
task, task s pre-image, task s dotazem do Dataverse, multi-entitní capability task,
přidání stepu k existujícímu pluginu, dopsání testu, refaktor příliš velkého tasku.
Pouštět po každé změně katalogu pravidel — to je jediná ochrana proti tomu, aby
„vylepšení“ instrukcí kvalitu nesnížilo.

---

## 12. Návrh postupu (fáze P0–P3)

### P0 — Základ (nejlepší poměr hodnoty a nákladů)
- `AGENTS.md` + tenké redirecty pro Copilot / Claude Code / Cursor.
- `docs/ai/rules/*` — katalog z §5 s ID pravidel.
- Sjednocení kanonických příkladů (§9.2) v `/docs` i `/examples`.
- Oprava nálezů z §9.3, jazykové značky u bloků kódu.
- `docs/ai/verify.md` s doslovnými příkazy.

### P1 — Vynucení a tooling (obsahuje prerekvizitu z Q3)
- **`pillaro new-step` — generátor step/image ID.** Prerekvizita zvolené GUID politiky (§6.1).
- Zprovoznění existujících offline příkazů `manifest` a `validate` v CLI routeru (§7.2/1)
  a rozšíření validátoru o odmítnutí GUIDů z dokumentace a příkladů.
- `-warnaserror` profil pro AI/CI běh.
- Pravidla `PF-ENV-*` pro práci s dev prostředím (§7.1) — nutná dřív, než agent dostane credentials.
- Skills / slash commands: `new-task`, `new-step`, `new-test`, `review-pf`.
- Doplnění chybějících konvencí z §9.4.

### P2 — Distribuce k zákazníkům (hlavní cíl podle Q1) + uzavření smyčky
- Instrukční pack do `dotnet new` šablony a VSIX šablony.
- Generovaný `Tools/AI/` v NuGet balíčku (stejný mechanismus jako `Tools/Deployment`).
- Publikační krok místo ručního kopírování pravidel (§3, důsledek 1).
- Smoke test v `scripts/Test-DotNetTemplateArtifacts.ps1`, že se pack propíše.
- Cesta Diagnostic Log → agent (§7.2/4) — dev prostředí už je k dispozici.

### P3 — Analýza jako vstup a měření
- Formát specifikace tasku (§8) + generátor.
- Eval sada a metriky (§11).
- Samostatná contributor sada pravidel nad stejným jádrem (§3, důsledek 3).

---

## 13. Otevřené otázky a chybějící vstupy

Body, které mění návrh natolik, že je nemá smysl domýšlet. Označené **(Q)** jsou
položeny přímo zadavateli.

**Q1–Q4 jsou zodpovězené** (viz §1.1) a zapracované. Zbývá:

1. **Formát vstupu z analýzy.** Existuje dnes ustálený formát analytických výstupů
   (Word, Azure DevOps work items, něco jiného)? Bez toho je §8 návrh naslepo.
2. **Konvence pojmenování stepů** — existuje interní dohoda, kterou jen zapsat, nebo ji
   máme navrhnout?
3. **Jazyk instrukcí.** Doporučujeme EN (kód i dokumentace jsou EN, balíčky jsou public).
   Pokud je požadavek na CZ, řešit jako druhou vrstvu, ne jako překlad pravidel.
4. **Public vs. interní.** Je instrukční sada součástí open-source repozitáře (marketingová
   hodnota „AI-ready“), nebo je to interní Pillaro standard?
5. **IP a compliance.** Existují u zákazníků omezení na použití AI nástrojů / posílání
   kódu do cloudových modelů, která musí instrukce zmiňovat?
6. **Vlastník a údržba.** Kdo katalog pravidel udržuje a při jaké události se aktualizuje
   (release? každá změna veřejného API?). Bez vlastníka drift nastane vždy.
7. **Identita dev prostředí** (navazuje na Q4) — jaká je URL dev prostředí, aby šlo
   implementovat pojistku `PF-ENV-006`, a kdo agentovi credentials poskytuje?
8. **Rozsah `pillaro new-step`** (navazuje na Q3) — má příkaz jen vrátit GUID, nebo přímo
   zapsat celý fluent blok do plugin classy a zarovnat `RegisterTask(...)`?

---

## ➡️ Související dokumenty

- [Architecture](../plugins/architecture.md)
- [Task Model](../plugins/task-model.md)
- [Validation Model](../plugins/validation.md)
- [Plugin Registration API](../plugins/plugin-registration-api.md)
- [Testing Overview](../tests/testing.md)
- [Contributing](../CONTRIBUTING.md)
- [Limitations](../limitations.md)
