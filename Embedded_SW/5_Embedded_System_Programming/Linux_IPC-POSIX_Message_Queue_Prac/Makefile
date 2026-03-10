CC      = gcc
CFLAGS  = -Wall -g
LDFLAGS = -lrt

.PHONY: all solution run clean

all: calc_server calc_client

calc_server: calc_server.c
	$(CC) $(CFLAGS) -o $@ $< $(LDFLAGS)

calc_client: calc_client.c
	$(CC) $(CFLAGS) -o $@ $< $(LDFLAGS)

solution:
	$(CC) $(CFLAGS) -o calc_server calc_server_solution.c $(LDFLAGS)
	$(CC) $(CFLAGS) -o calc_client calc_client_solution.c $(LDFLAGS)

run:
	./calc_server &
	sleep 0.3
	./calc_client
	wait

clean:
	rm -f calc_server calc_client
	rm -f /dev/mqueue/calc_req /dev/mqueue/calc_resp
