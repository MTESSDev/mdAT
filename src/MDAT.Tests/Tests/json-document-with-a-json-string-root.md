# Json_document_with_a_json_string_root

> Regression: a JsonDocument whose root is a string holding JSON text is expected as a YAML object (mdAT 1.4 behaviour).

## String root holding a JSON object

The document root is the string `{"UneCle": ...}`: the expected value is the YAML object.

``````yaml
donnees: '{"UneCle": "UneValeur", "UneAutreCle": "UneAutreValeur"}'
racineEstUneChaine: true
expected:
  verify:
    - type: match
      jsonPath: $[0].Donnees
      data:
        UneCle: UneValeur
        UneAutreCle: UneAutreValeur
``````

## Object root

The other form of the same data: the root of the document is the object itself. Same expected value.

``````yaml
donnees: '{"UneCle": "UneValeur", "UneAutreCle": "UneAutreValeur"}'
racineEstUneChaine: false
expected:
  verify:
    - type: match
      jsonPath: $[0].Donnees
      data:
        UneCle: UneValeur
        UneAutreCle: UneAutreValeur
``````

## String root holding a JSON array

``````yaml
donnees: '[1, 2, {"UneCle": null}]'
racineEstUneChaine: true
expected:
  verify:
    - type: match
      jsonPath: $[0].Donnees
      data:
        - 1
        - 2
        - UneCle: null
``````

## Whole recorded call

The complete call, as the real test checks it: every property, `Donnees` being a YAML object.
This is the case that failed with mdAT 1.5.0 to 2.0.0-beta.1: when `jsonPath` targets `Donnees` directly, a string
holding JSON text was already read as JSON, but inside a larger expected object it stayed a string.

``````yaml
donnees: '{"UneCle": "UneValeur", "UneAutreCle": "UneAutreValeur"}'
racineEstUneChaine: true
expected:
  verify:
    - type: match
      jsonPath: $
      data:
        - Gabarit: UnGabarit
          Donnees:
            UneCle: UneValeur
            UneAutreCle: UneAutreValeur
          Options:
            Format: pdf
          ProprietesDocument:
            Titre: Un titre
``````

## Sub-property holding JSON text stays a string (string root)

Only the root of the document is read as JSON. In the object, `Imbrique` is a string that happens to hold JSON text:
it stays a string, so it is expected as a string.

``````yaml
donnees: '{"UneCle": "UneValeur", "Imbrique": "{\"x\": 1}"}'
racineEstUneChaine: true
expected:
  verify:
    - type: match
      jsonPath: $
      data:
        - Gabarit: UnGabarit
          Donnees:
            UneCle: UneValeur
            Imbrique: '{"x": 1}'
          Options:
            Format: pdf
          ProprietesDocument:
            Titre: Un titre
``````

## Sub-property holding JSON text stays a string (object root)

Same data when the root of the document is the object itself.

``````yaml
donnees: '{"UneCle": "UneValeur", "Imbrique": "{\"x\": 1}"}'
racineEstUneChaine: false
expected:
  verify:
    - type: match
      jsonPath: $
      data:
        - Gabarit: UnGabarit
          Donnees:
            UneCle: UneValeur
            Imbrique: '{"x": 1}'
          Options:
            Format: pdf
          ProprietesDocument:
            Titre: Un titre
``````

## String root that is not JSON stays a string

A plain string, or a text that only looks like JSON, is not turned into an object.

``````yaml
donnees: bonjour
racineEstUneChaine: true
expected:
  verify:
    - type: match
      jsonPath: $[0].Donnees
      data: bonjour
``````

## Old form still accepted: expected as a JSON string

Written as JSON text, the expected object is still compared as an object.

``````yaml
donnees: '{"UneCle": "UneValeur"}'
racineEstUneChaine: true
expected:
  verify:
    - type: match
      jsonPath: $[0].Donnees
      data: '{"UneCle": "UneValeur"}'
``````
