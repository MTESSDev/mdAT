# T

> C# : [AdapterTests.cs:46](../AdapterTests.cs#L46) <!-- mdat:AdapterTests.cs#AdapterTests.Expected_with_verification_steps -->

## Case 1

first

``````yaml
input:
  a: 5
expected:
  verify:
    - type: match
      jsonPath: $.a
      data: [1, 2]
``````

## Case 2

second

``````yaml
input:
  a: 6
expected:
  verify:
    - type: match
      jsonPath: $.a
      data: {x: 1}
``````
