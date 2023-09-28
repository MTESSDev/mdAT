# Demo tests
## Cas 1.0
``````yaml skipped
val1: 1
val2: 2
expected: eeeeee
``````

### Cas 1.1
``````yaml --skipped
val1: 212
val2: 2444
``````

#### Cas 1.2.2
``````yaml
val1: 000000000
val2: 000000000
expected: 0
``````

#### Cas 1.2.2.1
``````yaml (skipped)
val1: 212
val2: 2444
expected: 23242353f25
``````

##### Cas 1.2.2.2.1
``````yaml [skipped]
val1: 212
val2: 2444
expected: 222222222222222222222222
``````

###### Cas 1.2.3.4.5.6 
``````yaml .skipped.
val1: 9999
expected: xtw22
``````

### Cas x
``````yaml  skipped
dsdsfde
``````

### Cas y
``````yaml				 skipped
regeg
``````