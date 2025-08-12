# MasRuze

Pro zobrazení obsahu souboru doporučuji využít webovou stránku: https://markdownlivepreview.com/

[Popis výstupních datasetu](../../../docs/internal-price-dataset.md)

- `Data`: Vstupní data.

  - `Data.csv`: Vstupní data s jednotlivými objekty (CP entitami).
  - `DataConsumptions.csv`: Data o spotřebách (identifikátor je číslo objektu ze souboru `Data.csv`).
  - `DataProductions.csv`: Data o výrobách (identifikátor je číslo objektu ze souboru `Data.csv`).

- `out__YYYY_MM_DD__HH_mm_SS`: Výstupní data (Rok_Měsíc_Den\_\_Hodina_Minuta_Sekunda).

  - `cp37_production10`: Objekt číslo `37` má bioplynovou stanicí, uvažujeme pouze 10 % výroby.

    - `all`: Všechny objekty.
    - `without18`: Všechny objekty kromě objektu číslo `18`.

  - `cp37_production100`: Objekt číslo `37` má bioplynovou stanicí, uvažujeme 100 % výroby.

    - `all`: Všechny objekty.
    - `without18`: Všechny objekty kromě objektu číslo `18`.

## Poznámky

- Objekt číslo `21` má bioplynovou stanicí, ale uvažujeme pouze 10 % výroby.
- Objekt číslo `18` má zásadně majoritní spotřebu, ale cena silové elektřiny je velice nízká. Nevíme tedy, zda se bude chtít účastnit sdílení (jako odběratel).
