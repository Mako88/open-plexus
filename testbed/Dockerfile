# A node, or the driver -- same image, different command.
#
# iproute2 is here for `tc`, which is how the link is impaired. It needs
# NET_ADMIN on the container; without that capability `tc` fails loudly rather
# than silently running an unimpaired experiment, which is the failure mode that
# would matter.
FROM python:3.13-slim

RUN apt-get update \
 && apt-get install -y --no-install-recommends iproute2 \
 && rm -rf /var/lib/apt/lists/*

RUN pip install --no-cache-dir numpy

WORKDIR /app
COPY openplexus /app/openplexus
COPY testbed /app/testbed

ENV PYTHONUNBUFFERED=1
