# Md_JsonDocument_test

> C# : [NewtonsoftConvertersTests.cs:14](../NewtonsoftConvertersTests.cs#L14) <!-- mdat:NewtonsoftConvertersTests.cs#NewtonsoftConvertersTests.Md_JsonDocument_test -->

> Test JsonDocument Load\
This test whole document integrity

## Case 1

Description

``````yaml
jsonDocument: {"demo": "json"}
test1: 
  JsonDocument: {"demo": "json"}
expected:
  name: null
  generateExpectedData: null
  verify: 
    - type: match
      jsonPath: $
      allowAdditionalProperties: false
      data: {"demo": "json"}
expected2:
  name: null
  generateExpectedData: null
  verify: 
    - type: match
      jsonPath: $
      allowAdditionalProperties: false
      data: 
        JsonDocument: 
          demo: json
``````