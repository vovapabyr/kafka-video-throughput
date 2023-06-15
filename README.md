# Kafka Video Throughput
## About
The main idea of this project is to investigate kafka throughput with sending video frames and see how number of partitions and parallel consumers affect it. By default, this setup configure kafka cluster with three nodes and zookeeper. It also starts [producer](src/video-frames-producer), configured number of [consumers](src/video-frames-consumer) and [analytic](src/video-frames-analytic) service. The data flow is next: 
 - prodcuer sends each frame of the video to kafka cluster 
 - consumers read the frames and sends frame metadata (index, size, ...) to another topic of the kafka cluster 
 - analytic service reads the frames' metadata from the kafka, calculates analytics (throughput and latency) and store in memmory
 - once the video is processed, user can go to the home page of the analytic service and download the analytic's data (throughput and latency) in csv files.

Also during processing, home page of analytic service display system's current throughput and latency. Charts displayed in Results section was built with the help of (https://www.csvplot.com/), where csv files could be uploaded. 

## How to run
Checkout the project and navigate to [src](src) folder. Before starting the project bind mount directory with the video on your host computer to direcotry that producer uses: [docker-compose.yml](src/docker-compose.yml) 
```
 videoframesproducer:
    image: videoframesproducer
    container_name: videoframesproducer
    build:
      context: .
      dockerfile: video-frames-producer/Dockerfile
    ports:
      - 5115:5115
    volumes:
      - "../data/videos:/app/videos"
    depends_on:
      init-kafka: 
        condition: service_completed_successfully
```
Also, you need specify video name in [appsettings.json](src/video-frames-producer/appsettings.json) of the producer:
```
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Kafka": {
    "BootstrapServers": "kafka-1:9092,kafka-2:9092,kafka-3:9092",
    "FramesTopicName": "frames-topic"
  },
  "VideoName" : "30_minute_countdown_timer.mp4"
}
```
Depending on the testing scenario we would run kafka topic with different number of partitions. Set desired number of topic's partitions in [docker-compose.yml](src/docker-compose.yml):
```
echo -e 'Creating kafka frames-topic topic'
kafka-topics --bootstrap-server kafka-1:9092 --create --if-not-exists --topic frames-topic --replication-factor 3 --partitions 1
```
And finally, to start the kafka cluster with producer, consumers and analytic services run: ```docker compose up --scale videoframesconsumer=10```, where ```videoframesconsumer=10``` defines number of consumers.  
## Results
### 1 partition, 1 consumer [throughput.csv](data/results/1-partition-1-consumer/Throughput.csv),[latency.csv](data/results/1-partition-1-consumer/Latency.csv) ~ 1hr to process 36_000 frames 
```--partitions 1``` + ```docker compose up --scale videoframesconsumer=1```
![partitions_consumers](https://github.com/vovapabyr/kafka-video-throughput/assets/25819135/0a7fbb9b-5efe-441c-8b44-b9c70eed09f0)
 - docker resources:
 ![docker_resources](https://github.com/vovapabyr/kafka-video-throughput/assets/25819135/f4bbbb56-4c8c-4dfe-8e06-5f0d2331bf8d)
 - avg throughput ~ 12 Mbps:
 ![throughput](https://github.com/vovapabyr/kafka-video-throughput/assets/25819135/92d0f9bd-5bff-458c-8d96-3bbd3ec45e15)
 - max end-to-end latency ~ 3_000_000 ms -> 3_000 sec -> 50 min:
 ![latency](https://github.com/vovapabyr/kafka-video-throughput/assets/25819135/5d631137-8624-414b-b703-84f3041f8c43)
### 1 partition, 2 consumers [throughput.csv](data/results/1-partition-2-consumers/Throughput.csv),[latency.csv](data/results/1-partition-2-consumers/Latency.csv) ~ 1hr to process 36_000 frames 
```--partitions 1``` + ```docker compose up --scale videoframesconsumer=2```
![partitions_consumers](https://github.com/vovapabyr/kafka-video-throughput/assets/25819135/48556888-43dc-4dd3-acb3-a2ffcd078800)
 - docker resources:
 ![docker_resources](https://github.com/vovapabyr/kafka-video-throughput/assets/25819135/95c6de06-0805-4fc9-9b55-6dbd6b214462)
 - avg throughput ~ 12 Mbps:
 ![throughput](https://github.com/vovapabyr/kafka-video-throughput/assets/25819135/101e10bd-7009-4ebe-abe3-9e1159af2e2e)
 - max end-to-end latency ~ 3_000_000 ms -> 3_000 sec -> 50 min:
 ![latency](https://github.com/vovapabyr/kafka-video-throughput/assets/25819135/b8e8bfce-e983-456c-9112-890c505cdfd6)
  As we can see a single partition can be conusmed only by one consumer, that's why one of the consumers doesn't do anything. So, the results should be pretty much the same as in '1 partition, 1 consumer' test case. 

 
