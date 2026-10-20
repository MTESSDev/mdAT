# JsonPath

> C# : [MarkdownTestAttributeTests.cs:319](../MarkdownTestAttributeTests.cs#L319) <!-- mdat:MarkdownTestAttributeTests.cs#MarkdownTestAttributeTests.JsonPath -->

## Case 1

Description

``````yaml
expected:
  generateExpectedData: null
  verify: 
  - type: match
    jsonPath: $.test.length()
    data: 2
``````


## Case 2

Test wildcard return

``````yaml
expected:
  generateExpectedData: null
  verify: 
  - type: match
    jsonPath: $.test[*]
    data: 
      - val1
      - val2
``````

