# V7MasRuze

## Slovníček

- `CP`: Consumption Production.
- `Pv`: Photovoltaic - Fotovoltaická elektrárna (FVE)
- `Bg`: Biogas - Bioplynová elektrárna (BP, nebo BPS)

## Základní pojmy

### Učastníci komunity

Učastnící komunity jsou členové komunity, kteří se podílejí na sdílení. Mají danou spotřebu a výrobu.

### Správci komunity

Spravci komunity řeší její administrativní fungování. Peníze dostávají z interního poplatku za sdílení. Musí hradit fixní náklady (zejména mzdové).

### Komunitní interní ceny

V komunitě jsou 3 typy cen, které určují cenu za sdílení. Vždy jedna z cen je závislá na předchozích dvou cenách.

- `InternalPriceBuy`: Cena za výkup elektřiny (Kč/kWh). Cena za kterou správci komunity vykupují elektřinu od výroben (účastníků komunity).
- `InternalPriceFee`: Poplatek za sdílení (Kč/kWh). Poplatek, který správci komunity přičtou k ceně za výkup. Tento poplatek vyvádí pěníze (Cash flow) od účastníků ke správcům komunity.
- `InternalPriceSell`: Cena za prodej elektřiny (Kč/kWh). Cena vytvořena součtem ceny za výkup a poplatku za sdílení. Za tuto cenu nakupují spotřebitelé (účastníci komunity) elektřinu v komunitě.

### Cena za výkup

Cena za výkup je cena, za kterou komunita vykupuje elektřinu.

### CP Entita

CP znamená Consumption Production. Jedná se tedy o entitu, která má danou spotřebu a výrobu. Má tedy v podstatě dva diagramy, jak spotřeby, tak výroby.

### Vyrovnání (Balancing)

Vyrovnání CP, neboli spotřeby a výroby defakto reprezentuje samospotřebu. V čase, kde je výroba a spotřeba se z výroby pokryje spotřeba. Tím se sníží výroba a spotřeba a buď zůstane v tomto čase přebytek výroby (přetok), nebo naopak nedostatek výroby (nedostatek). Tyto přebytky a nedostatky se dají využít například při sdílení, nebo pokud sdílení není, tak se přebytky prodají do sítě.

## Dataset `out`

- `M1CurrentStateTechnial`

  - `M1DiagramsEach`: Diagramy pro jednotlivé CP entity.

    - Pro každou CP entitu jsou vytvořeny následující diagramy příslušného energetického stavu elektřiny (každá část obsahuje spotřebu (Consumtion) a výrobu (Production)):
      - `Initial`: Počáteční spotřeba a produkce (před samostpotřebou a zahrnutím sdílení).
      - `InitialBalanced`: Počáteční spotřeba a produkce po vyrovnáním (před samostpotřebou a zahrnutím sdílení). Zde dojde vyrovnání spotřeby a produkce. Lze takto identifikovat samospotřebu.
      - `FinalSystem`: Spotřeba a produkce po zahrnutí systémů. V tomto případě bude stejné jako InitialBalanced, jelikož systémy nejsou zahrnuté, ale produkce fve a bps je zahrnuta v manuálně zadaném vektoru produkce, který je již v Initial stavu.
      - `Final`: Koncová spotřeba a produkce (po samostpotřebě a sdílení). Je uvažována dynamický metoda sdílené.
    - Dále pro každou CP entitu jsou vytvořeny následující diagramy:
      - `InitialConsumptionTDD`: Počáteční spotřeba jak by vypadala podle odhadu TDD.
      - `InitialConsumptionTDDScaled`: Počáteční spotřeba jak by vypadala podle odhadu TDD. Časová granualita dat stejná jako u Initial -> Consumption diagramů.

  - `M2DiagramsTotal`: Agregované diagramy pro více CP entit.

    - `Bg`: Spotřeba a produkce entit s bioplynovou stanicí.
    - `BgBalanced`: Spotřeba a produkce entit s bioplynovou stanicí po vyrovnání.
    - `BgBalancedSeparetely`: Spotřeba a produkce entit s bioplynovou stanicí po vyrovnání odděleně u každé CP entity.
    - `Comparison`: Porovnání spotřeby a výroby fve a bps.
    - `ConsumptionWithoutBg`: Spotřeba bez entit s bioplynovou stanicí.
    - `ProductionCombined`: Porovnání produkce fve a bps.
    - `Pv`: Spotřeba a produkce entit s FVE.
    - `PvBalanced`: Spotřeba a produkce entit s FVE po vyrovnání.
    - `PvBalancedSeparetely`: Spotřeba a produkce entit s FVE po vyrovnání odděleně u každé CP entity.
    - `Total`: Celková spotřeba a produkce.
    - `TotalBalanced`: Celková spotřeba a produkce po vyrovnání.

- `M2CurrentStateEconomy`: Ekonomické diagramy s cenami, zelenými bonusy a hotovostními toky z prodejů.

  - `CashFlowSell`:
  - `CashFlowSellSorted`:
  - `CashFlowSellWithGreenBonus`:
  - `CashFlowSellWithGreenBonusSorted`:
  - `GreenBonus`:
  - `GreenBonusSorted`:
  - `GreenBonusYearTo`:
  - `GreenBonusYearToSorted`:
  - `SellPrice`:
  - `SellPriceSorted`:
  - `SellPriceWithGreenBonus`:
  - `SellPriceWithGreenBonusSorted`:

- `M3Ladders`: Žebříčky zapojení zdrojů při dané cenové hladině výkupu.

  - `AnnualProduction`: Velikost roční produkce v komunitě při dané cenové hladině výkupu + porovnání se spotřebou celé komunity.
  - `InstalledPower`: Velikost instalovaného výkonu v komunitě při dané cenové hladině výkupu.

- `M4InternalPrice`

  - `InternalPriceBuy`: Závislost NPV na vnitřní ceně za výkup (cena od které komunita bude vykupovat výrobu) - Závislost musí být konstantní.
  - `InternalPriceFee`: Závislost NPV na velikosti poplatku (k ceně za výkup se přičítá poplatek, který člen platní za přijetí sdílené energie -> odvádí od účastníků ke správcům komunity).

- `M5LCOE`

  - `M1Optimization`: Optimizace vnitřní ceny za výkup.
  - `M2Optimization`: Optimizace velikosti poplatku.
