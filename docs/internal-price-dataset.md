# Internal Price Dataset

Pro zobrazení obsahu souboru doporučuji využít webovou stránku: https://markdownlivepreview.com/

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

### Consumption Coefficient (kWh/kWp)

Consumption Coefficient je koeficient, který určuje velikost spotřeby vzhledem k instalovanému výkonu FVE.

- Například pokud je koeficient roven 1, tak na 1 kWp instalovaného výkonu FVE je přidána k dané CP entitě 1 MWh roční spotřeby.
- Například pokud je koeficient roven 0.5, tak na 1 kWp instalovaného výkonu FVE je přidána k dané CP entitě 0.5 MWh roční spotřeby.
- Například pokud je koeficient roven 2, tak na 1 kWp instalovaného výkonu FVE je přidána k dané CP entitě 2 MWh roční spotřeby.

`Roční spotřeba (kWh)` = `Consumption Coefficient (kWh/kWp)` \* `Instalovaný výkon FVE (kWp)` \* `1000`

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

  - `CashFlowSell`: Roční hotovostní toky z prodeje elektřiny (mimo sdílení).
  - `CashFlowSellSorted`: Roční hotovostní toky z prodeje elektřiny (mimo sdílení) seřazené podle velikosti.
  - `CashFlowSellWithGreenBonus`: Roční hotovostní toky z prodeje elektřiny (mimo sdílení) s zahrnutím zeleného bonusu.
  - `CashFlowSellWithGreenBonusSorted`: Roční hotovostní toky z prodeje elektřiny (mimo sdílení) s zahrnutím zeleného bonusu seřazené podle velikosti.
  - `GreenBonus`: Hodnoty zeleného bonusu.
  - `GreenBonusSorted`: Hodnoty zeleného bonusu seřazené podle velikosti.
  - `GreenBonusYearTo`: Rok, do kdy je zelený bonus platný.
  - `GreenBonusYearToSorted`: Rok, do kdy je zelený bonus platný seřazený podle velikosti.
  - `SellPrice`: Prodejní cena elektřiny (mimo sdílení).
  - `SellPriceSorted`: Prodejní cena elektřiny (mimo sdílení) seřazená podle velikosti.
  - `SellPriceWithGreenBonus`: Prodejní cena elektřiny (mimo sdílení) s zahrnutím zeleného bonusu.
  - `SellPriceWithGreenBonusSorted`: Prodejní cena elektřiny (mimo sdílení) s zahrnutím zeleného bonusu seřazená podle velikosti.

- `M3Ladders`: Žebříčky zapojení zdrojů při dané cenové hladině výkupu.

  - `AnnualProduction`: Velikost roční produkce v komunitě při dané cenové hladině výkupu + porovnání se spotřebou celé komunity.
  - `InstalledPower`: Velikost instalovaného výkonu v komunitě při dané cenové hladině výkupu.

- `M4InternalPrice`: Vnitřní ceny.

  - `InternalPriceBuy`: Závislost NPV na vnitřní ceně za výkup (cena od které komunita bude vykupovat výrobu) - Závislost musí být konstantní.
  - `InternalPriceFee`: Závislost NPV na velikosti poplatku (k ceně za výkup se přičítá poplatek, který člen platní za přijetí sdílené energie -> odvádí od účastníků ke správcům komunity).

- `M5LCOE`: Jednotkové náklady na sdílenou elektřinu. (Počítáno s fixní částí za rok a variabilní částí za kWh.)

  - `M1LCOE`: Jednotkové náklady na sdílenou elektřinu.

    - `All`: Plný range velikosti sdílené elekřiny (osa x).
    - `All2`: Mírně zúžený range velikosti sdílené elekřiny (osa x).
    - `All2Targets`: Mírně zúžený range velikosti sdílené elekřiny (osa x) s cílovými hodnotami.
    - `AllTargets`: Plný range velikosti sdílené elekřiny (osa x) s cílovými hodnotami.
    - `AllUnlabeled`: Plný range velikosti sdílené elekřiny (osa x) bez hodnot výroby a spotřeby.
    - `AllUnlabeledTargets`: Plný range velikosti sdílené elekřiny (osa x) bez hodnot výroby a spotřeby s cílovými hodnotami.
    - `EnergyHigh`: Range velikosti sdílené elekřiny (osa x) pro velké hodnoty.
    - `EnergyHighTargets`: Range velikosti sdílené elekřiny (osa x) pro velké hodnoty s cílovými hodnotami.
    - `EnergyLow`: Range velikosti sdílené elekřiny (osa x) pro malé hodnoty.
    - `EnergyLow2`: Range velikosti sdílené elekřiny (osa x) pro malé hodnoty.
    - `EnergyLow2Targets`: Range velikosti sdílené elekřiny (osa x) pro malé hodnoty s cílovými hodnotami.
    - `EnergyLowTargets`: Range velikosti sdílené elekřiny (osa x) pro malé hodnoty s cílovými hodnotami.

  - `M2LCOESensitivity`: Jednotkové náklady na sdílenou elektřinu pro různé velikosti fixní části.

    - Stejné jako u M1LCOE, ale pro různé velikosti fixní části.

  - `M3LCOEConsumer`: Ekonomická hodnota sdílení pro odběratele.

    - `Consumer`: Ekonomická hodnota sdílení pro odběratele při různých rozdílench mezi cenou za silovou elektřinu a prodejní cenou v komunitě.

- `M6Potential`:

  - `M1WithoutBg`: Potenciál sdílení bez bioplynových stanic. (`<CC>` - consumption coefficient)

    - `NewPvEnergyShared_ConsumptionCoefficient<CC>`: Závislost množství sdílené elektřiny na přidaném instalovaném výkonu FVE pro různé spotřeby se zadaným consumption coefficient.
    - `NewPvEnergyShared_ConsumptionCoefficient<CC>_Targets`: Závislost množství sdílené elektřiny na přidaném instalovaném výkonu FVE pro různé spotřeby se zadaným consumption coefficient s cílovými hodnotami.
    - `NewPvNPV_ConsumptionCoefficient<CC>`: Závislost NPV na přidaném instalovaném výkonu FVE pro různé spotřeby se zadaným consumption coefficient.

  - `M2WithBg`: Potenciál sdílení s bioplynovými stanicemi.

    - `NewConsumptionEnergyShared`: Závislost množství sdílené elektřiny na přidané spotřebě pro různé diagramy TDD.
    - `NewConsumptionEnergySharedDetail`: Závislost množství sdílené elektřiny na přidané spotřebě pro různé diagramy TDD v detailu.
    - `NewConsumptionEnergySharedDetailTargets`: Závislost množství sdílené elektřiny na přidané spotřebě pro různé diagramy TDD v detailu s cílovými hodnotami.
    - `NewConsumptionEnergySharedTargets`: Závislost množství sdílené elektřiny na přidané spotřebě pro různé diagramy TDD s cílovými hodnotami.
    - `NewConsumptionNPV`: Závislost NPV na přidané spotřebě pro různé diagramy TDD.

- `M7Case`:

  - `M1Current`: Aktuální stav komunity.

    - `AnnualCashFlow`: Roční hotovostní toky variant.
    - `NPV`: NPV variant.

  - `M2WithBg`: Varianty s bioplynovými stanicemi.

    - `AnnualCashFlow`: Roční hotovostní toky variant.
    - `AnnualCashFlow_<i>`: Roční hotovostní toky i-té části.
    - `NPV`: NPV variant.
    - `PresentValues_<i>`: Současné hodnoty i-té části.

  - `M3WithoutBg`: Varianty bez bioplynových stanic.

    - `AnnualCashFlow`: Roční hotovostní toky variant.
    - `AnnualCashFlow_<i>`: Roční hotovostní toky i-té části.
    - `NPV`: NPV variant.
    - `PresentValues_<i>`: Současné hodnoty i-té části.
