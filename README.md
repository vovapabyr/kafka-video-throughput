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
