# Are lobsters spiders? - Finding taxonomy relations using linked data

This repository is a school project for the semantic web course. The goal is to
find a method for answering a question that is easy for a human, but difficult
for a computer: are lobsters spiders?

Our solution and implementation is described in
[are_lobsters_spiders.pdf](are_lobsters_spiders.pdf).

## Prerequisites

- `Docker`
- The NCBI taxonomy ontology: [http://purl.obolibrary.org/obo/ncbitaxon.owl]()
(unless the SPARQL endpoint on [http://134.209.17.65:3030/species/query]() is
still running)

## Creating the SPARQL endpoint

This part can be skipped if the SPARQL endpoint on
[http://134.209.17.65:3030/species/query]() is available.

<!-- TODO -->
...

## Building and running the tool

`cd AreLobstersSpiders`

`docker build . -t are_lobster_spiders`

`docker run --rm are_lobster_spiders isa DBpedia_URI_A DBpedia_URI_B`

Some examples:

```
docker run --rm are_lobster_spiders isa \
    http://dbpedia.org/resource/Lobster \
    http://dbpedia.org/resource/Spider

docker run --rm are_lobster_spiders isa \
    http://dbpedia.org/resource/Philaeus \
    http://dbpedia.org/resource/Spider

docker run --rm are_lobster_spiders isa \
    http://dbpedia.org/resource/Dog \
    http://dbpedia.org/resource/Cat

docker run --rm are_lobster_spiders isa \
    http://dbpedia.org/resource/Dog \
    http://dbpedia.org/resource/Mammal
```