#!/bin/bash

echo "Testing load balancing - 6 requests to tickets-service/instance:"
echo ""

kubectl run --rm -it --image=curlimages/curl demo --restart=Never -- \
  sh -c 'for i in 1 2 3 4 5 6; do curl -s http://tickets-service/instance; echo; done'
