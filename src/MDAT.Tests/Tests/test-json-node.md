# Test_JsonNode

> C# : [NewtonsoftConvertersTests.cs:30](../NewtonsoftConvertersTests.cs#L30) <!-- mdat:NewtonsoftConvertersTests.cs#NewtonsoftConvertersTests.Test_JsonNode -->

> Test SystemTextJsonFullConverter

## Case 1

Description

``````yaml
nodes: 
  - 1.2
  - test
expected:
  name: null
  generateExpectedData: null
  verify: 
    - type: match
      jsonPath: $
      allowAdditionalProperties: false
      data: 
        - 1.2
        - test
``````