# Are lobsters spiders? - Finding taxonomy relations using linked data

This repository is a school project for the semantic web course. The goal is to
find a method for answering a question that is easy for a human, but difficult
for a computer: are lobsters spiders?

Our solution and implementation is described in
[are_lobsters_spiders.pdf](are_lobsters_spiders.pdf).

## Prerequisites

- Docker
- The NCBI taxonomy ontology: [http://purl.obolibrary.org/obo/ncbitaxon.owl](http://purl.obolibrary.org/obo/ncbitaxon.owl)
(unless the SPARQL endpoint on [http://134.209.17.65:3030/species/query](http://134.209.17.65:3030/species/query) is
still running)

## Creating the SPARQL endpoint

This part can be skipped if the SPARQL endpoint on
[http://134.209.17.65:3030/species/query](http://134.209.17.65:3030/species/query) is available.

```
wget http://purl.obolibrary.org/obo/ncbitaxon.owl
docker run --name fuseki-data -v /fuseki busybox
docker run -d --name fuseki -p 3030:3030 --volumes-from fuseki-data stain/jena-fuseki
docker logs fuseki
```

- Go to `http://<your_ip>:3030/`
- Enter the credentials displayed in the fuseki logs
- Go to 'manage datasets'
- Click on 'add new dataset'
- Enter the name 'species' and check 'Persistent'

```
docker stop fuseki
docker run --volumes-from fuseki-data -v $(pwd):/staging \
    stain/jena-fuseki ./load.sh species ncbitaxon.owl
```

There are approximately 12 million triples to add so it may take a while.

```
docker start fuseki
```

## Building and running the tool

If a SPARQL endpoint other than [http://134.209.17.65:3030/species/query](http://134.209.17.65:3030/species/query) is used: replace the URL in `AreLobstersSpiders/Program.cs:18`.

`cd AreLobstersSpiders`

`docker build . -t are_lobsters_spiders`

`docker run --rm are_lobsters_spiders isa DBpedia_URI_A DBpedia_URI_B`

Some examples:

```
docker run --rm are_lobsters_spiders isa \
    http://dbpedia.org/resource/Lobster \
    http://dbpedia.org/resource/Spider

docker run --rm are_lobsters_spiders isa \
    http://dbpedia.org/resource/Philaeus \
    http://dbpedia.org/resource/Spider

docker run --rm are_lobsters_spiders isa \
    http://dbpedia.org/resource/Dog \
    http://dbpedia.org/resource/Cat

docker run --rm are_lobsters_spiders isa \
    http://dbpedia.org/resource/Dog \
    http://dbpedia.org/resource/Mammal
```
