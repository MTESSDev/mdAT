---
title: Categories and properties of the cases
tags:
  category: Facturation
  priorite: Haute
  domaine: Taxes
---
# Md_Categories

> C# : [CategoryTests.cs:13](../CategoryTests.cs#L13) <!-- mdat:CategoryTests.cs#CategoryTests.Md_Categories -->

## File only

``````yaml
name: file
expectedCategories: Facturation
expectedProperties: domaine=Taxes;priorite=Haute
``````

## With a category

``````yaml category=Smoke,Rapide
name: case
expectedCategories: Facturation,Rapide,Smoke
expectedProperties: domaine=Taxes;priorite=Haute
``````

## Quoted, and a property of the file replaced

``````yaml category="Smoke" priorite=Basse
name: other
expectedCategories: Facturation,Smoke
expectedProperties: domaine=Taxes;priorite=Basse
``````

## Ignored, with its tags

Ignored: the test method never receives this case. It still has the tags of the file and its own.

``````yaml skipped category=Lent priorite=Basse
name: ignored
expectedCategories: Facturation,Lent
expectedProperties: domaine=Taxes;priorite=Basse
``````

## Ignored, tags first

``````yaml category=Lent priorite=Basse skipped
name: ignored too
expectedCategories: Facturation,Lent
expectedProperties: domaine=Taxes;priorite=Basse
``````

## Ignored, not even finished

``````yaml skipped category=Brouillon
name: unfinished
``````
