# Test_JsonElement

> Test SystemTextJsonFullConverter - JsonElement

## Case 1

Description

``````yaml
element: 
    ceci:
       est: 
          - un
          - objet : complet
expected:
  name: null
  generateExpectedData: null
  verify: 
    - type: match
      jsonPath: $
      allowAdditionalProperties: false
      data: 
        ceci:
            est: 
                - un
                - objet : complet
``````