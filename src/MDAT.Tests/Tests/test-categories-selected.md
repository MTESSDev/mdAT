---
tags:
  category: Facturation
---
# Md_Selected

> C# : [CategoryTests.cs:33](../CategoryTests.cs#L33) <!-- mdat:CategoryTests.cs#CategoryTests.Md_Selected -->

## Not selected

Ignored: another case of the file is selected.

``````yaml category=Autre
name: other
expectedCategories: Facturation,Autre
``````

## Selected

The only case of the file that runs.

``````yaml selected category=Focus
name: focus
expectedCategories: Facturation,Focus
``````

## Selected but skipped

Ignored, and it selects nothing.

``````yaml selected skipped category=Jamais
name: never
expectedCategories: Facturation,Jamais
``````

## Not selected either, not even finished

``````yaml category=Brouillon
name: unfinished
``````
