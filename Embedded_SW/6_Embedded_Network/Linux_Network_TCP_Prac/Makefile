CC      = gcc
CFLAGS  = -Wall -g

.PHONY: all solution run clean

all: calc_server calc_client

calc_server: calc_server.c
	$(CC) $(CFLAGS) -o $@ $<

calc_client: calc_client.c
	$(CC) $(CFLAGS) -o $@ $<

solution:
	$(CC) $(CFLAGS) -o calc_server calc_server.c
	$(CC) $(CFLAGS) -o calc_client calc_client.c

run:
	./calc_server &
	sleep 0.3
	./calc_client
	wait

clean:
	rm -f calc_server calc_client
