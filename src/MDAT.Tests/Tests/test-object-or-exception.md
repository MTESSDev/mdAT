# TestObjectOrException

> C# : [MarkdownTestAttributeTests.cs:329](../MarkdownTestAttributeTests.cs#L329) <!-- mdat:MarkdownTestAttributeTests.cs#MarkdownTestAttributeTests.TestObjectOrException -->

> Test jsonPath array count

## Case 1

Description

``````yaml
formulaireWebFRW1DO:
  Exception: 
    ClassName: System.DataMisalignedException
    ObjectName: null
    Message: Test Crashed
    ParamName: null
expected:
  generateExpectedData: null
  verify: 
    - type: match
      allowAdditionalProperties: true
      data:
        ClassName: System.DataMisalignedException
``````