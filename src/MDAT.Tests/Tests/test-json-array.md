# Test_JsonArray

> C# : [NewtonsoftConvertersTests.cs:54](../NewtonsoftConvertersTests.cs#L54) <!-- mdat:NewtonsoftConvertersTests.cs#NewtonsoftConvertersTests.Test_JsonArray -->

> Test SystemTextJsonFullConverter - JsonArray

## Case 1

Description

``````yaml
array:
  - t1
  - t2
  - t3 
expected:
  name: null
  generateExpectedData: null
  verify: 
    - type: match
      jsonPath: $
      allowAdditionalProperties: false
      data: 
          - t1
          - t2
          - t3 
``````