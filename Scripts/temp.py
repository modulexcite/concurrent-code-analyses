#!/usr/bin/python

import urllib2
import argparse
import json


json = json.loads(open("/Users/semih/Desktop/ex.json").read())

first_row = json

def traverse(o):
    for key, value in o.iteritems():
        if isinstance(value, dict):
            for key, value in traverse(value):
                yield (key, value)
        else:
            yield (key, value)

all_keys = list((key for key, value in traverse(first_row)))

print(",".join(all_keys))

for row in json["rows"]:
    data = dict(traverse(row))
    values = (str(data[key]) for key in all_keys)
    print(",".join(values))
