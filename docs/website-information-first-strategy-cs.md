# Web Pillaro: informační vstupní stránky místo „prodejních“ stránek

Tento dokument převádí stávající positioning Pillaro do podoby webu, který:

- nepůsobí jako agresivní prodej,
- rychle pomůže návštěvníkům pochopit „pro koho to je / není",
- dá rozhodovatelům i technickým lidem konkrétní informace,
- přirozeně vede ke kvalitní poptávce.

## 1) Co už dnes funguje (zachovat)

Z aktuálního webu je silně čitelné:

- **jasné zaměření** na MS D365 + Power Platform,
- **problémově orientovaná komunikace** (krize projektu, chaos, governance, onboarding),
- **3 pilíře nabídky** (Delivery Framework, Governance, Konzultace),
- **bezplatná úvodní konzultace** jako nízkoprahový vstup.

To je správný základ a neměl by se rozbít redesignem.

## 2) Hlavní změna: z „nabízíme službu“ na „pomáháme rozhodnout“

Primární cíl homepage:

> Uživatel do 60–90 sekund zjistí, jestli je Pillaro pro jeho situaci relevantní.

To znamená posílit prvky:

1. **Diagnostika situace** (rychlý rozcestník podle problému),
2. **Důkaz a transparentnost** (jak postupujete, co bude výstup),
3. **Kvalifikace návštěvníka** (kdy dává spolupráce smysl a kdy ne).

## 3) Doporučená informační architektura (Webflow)

## Homepage (informační)

1. **Hero = orientace, ne slogan**
   - Nadpis: „Pomáháme firmám stabilizovat a řídit rozvoj D365/Power Platform.“
   - Podnadpis: „Zjistěte během 3 minut, kde je hlavní riziko vašeho prostředí a jaký další krok dává smysl.“
   - CTA primární: „Diagnostika situace“
   - CTA sekundární: „Jak pracujeme“

2. **Rychlý rozcestník „Vaše situace“ (5–7 karet)**
   - Projekt v krizi
   - Chaos bez pravidel
   - Nekvalitní výstupy
   - Závislost na dodavateli
   - Potřebujeme onboarding/CoE

3. **Jak poznáte, že jsme fit (A/B fit sekce)**
   - „Spolupráce dává smysl, když…“
   - „Spolupráce nedává smysl, když…“

4. **Jak vypadá první měsíc spolupráce**
   - Týden 1: orientace + mapování rizik
   - Týden 2: prioritizace + governance minimum
   - Týden 3–4: stabilizační plán + role + metriky

5. **Důkazy bez přehnaného claimu**
   - 2–3 mini případovky (situace → zásah → výsledek)
   - reference na role (CIO, IT manager, product owner)

6. **FAQ pro rozhodovatele**
   - Jaký je minimální rozsah?
   - Co musí dodat klient?
   - Jak rychle poznáme přínos?
   - Umíte navázat na existujícího dodavatele?

7. **CTA na závěr (neagresivní)**
   - „Nejste si jistí? Začněme 30min orientačním callem.“

## Povinné podstránky

- **/pro-koho** (segmentace + anti-persona)
- **/jak-pracujeme** (proces, artefakty, role)
- **/situace/[slug]** (detail každé problémové situace)
- **/pripadove-studie** (strukturované case studies)
- **/slovnik-pojmu** (governance, DLP, CoE, ALM, ownership model)
- **/faq**

## 4) Struktura textu: „Situace → Dopad → Diagnostika → Krok“

Pro každou sekci i podstránku držet šablonu:

1. **Situace**: co se typicky děje
2. **Dopad**: jaké to má byznysové následky
3. **Diagnostika**: podle čeho to objektivně poznat
4. **Doporučený první krok**: co udělat do 14 dní

Tím se web stává praktickým nástrojem, ne katalogem služeb.

## 5) Copywriting pravidla pro „neprodejní“ tón

- Kratší věty, méně abstraktních slov („udržitelný“, „stabilní“) bez důkazu.
- Každý claim doplnit o **jak to poznám** nebo **co je výstup**.
- Omezit superlativy, zvýšit konkrétnost („workshop 2h“, „risk mapa“, „RACI draft“).
- Psát pro dvě persony současně:
  - management (riziko, odpovědnost, náklady)
  - delivery/IT (proces, standardy, kvalita)

## 6) Návrh konkrétních Webflow komponent

- **CMS kolekce „Situace“**: název, symptomy, dopady, metriky, první krok, CTA.
- **CMS kolekce „Případové studie“**: výchozí stav, zásah, výsledek, role klienta, velikost týmu.
- **Komponenta „Fit check“**: 2 sloupce (fit / nefit).
- **Komponenta „První měsíc“**: timeline.
- **Komponenta „Artefakty“**: co klient dostane (např. governance baseline, roadmapa, odpovědnosti).

## 7) Metriky kvality webu (ne jen konverze)

Měřit minimálně:

- CTR z homepage na stránky „situace“
- Scroll depth na „jak pracujeme“
- Dokončení FAQ (75 %+)
- Podíl poptávek, které odpovídají ideálnímu profilu klienta
- Počet callů, kde už klient správně pojmenuje problém

## 8) 30denní implementační plán

## Týden 1
- Sepsat 5–7 „situací“ a anti-situací.
- Definovat „fit / nefit“ kritéria.
- Připravit šablonu případovky.

## Týden 2
- Přepsat homepage do informační struktury.
- Vytvořit 3 detailní stránky situací.
- Nasadit FAQ a stránku „Jak pracujeme“.

## Týden 3
- Dopsat 2–3 případovky.
- Doplnit konkrétní výstupy spolupráce.
- Vyladit interní prolinkování a CTA.

## Týden 4
- Spustit měření (GA4 / hotjar / eventy).
- Vyhodnotit chování návštěvníků.
- Upravit hero, rozcestník a FAQ podle dat.

## 9) Co z pohledu značky Pillaro explicitně komunikovat

- Neprodáváte „vývoj na hodiny“, ale **řízení rizika a kvality rozvoje platformy**.
- Hodnota je v **know-how, standardech a přenosu kompetencí** do klienta.
- Cíl je **snižovat závislost** na externích dodavatelích, ne ji prohlubovat.

To je velmi silná diferenciace a na webu má být čitelná během prvních sekund.
