import torch
import torch.nn as nn
import torchvision.transforms as transforms
import matplotlib
import matplotlib.pyplot as plt
import socket
import struct
import traceback
import logging
import time
import numpy as np
import uuid
from heapq import nlargest
import random


#initialize models info dict
model_dict = {}
#hof for generation
hof = []
#GA variables
mutprob = 0.4


def sending_and_reciveing(model):
    s = socket.socket()
    socket.setdefaulttimeout(None)
    port = 60000
    s.bind(('127.0.0.1', port)) #local host
    empty_message = [0.0,0.0,0.0] # for when an empty message needs to be sent
    s.listen(10) #the number of unaccepted connections that the system will allow before refusing new connections
    print('socket  has started listensing ... ')
    while True:
        try:
            #connecting to unity through socket
            connection, addr = s.accept() #when port connected
            bytes_received = connection.recv(4000) #received bytes
            array_received = np.frombuffer(bytes_received, dtype=np.float32) #converting into float array
            
            #Something here to decern between the different types of data being sent

            #get data without flag
            data = array_received[1:]
            
            if array_received[0] == -1.0:
                Tensor_of_visual = Convert_visual_array_to_tensor(data)
                output = model(Tensor_of_visual)
                Decision = output.detach().numpy()[0]

                #sending of data
                bytes_to_send = struct.pack('%sf' % len(Decision), *Decision) #converting float to byte
                connection.sendall(bytes_to_send) #sending back
                connection.close()
                ###


                ## if recieving boid data
            elif array_received[0] == -2.0:
                #Debugging
                ##print("array_received: {0}".format(array_received))
                fitness = data[0]
                boid_id = data[1]
                generation = data[2]
                print("boid[{0}] : average fitness: {1}".format(boid_id,fitness))
                ui_for_boid = uuid.uuid4()
                torch.save(model.state_dict(), "boid_models/" + str(ui_for_boid) + ".pt")
                #change this to average fitness:
                model_dict[ui_for_boid] = [boid_id,fitness,generation]
                if generation == 0:
                    model = CNN()
                    ##get random weights for model if its the first generation
                    model.apply(weight_init)
                else:
                    choice_of_model = random.choice(hof)
                    model = CNN()
                    model.load_state_dict(torch.load("boid_models/" + str(choice_of_model) + ".pt"))
                    #mutate the model if its not the first generation
                    mutate_model()
                
                #savemodel with uuid and fitness of model with generation number
                bytes_to_send = struct.pack('%sf' % len(empty_message), *empty_message) #converting float to byte
                connection.sendall(bytes_to_send) #sending back
                connection.close()


            #when signalling new generation
            elif array_received[0] == -3.0:
                #get data
                fitness = data[1]
                boid_id = data[2]
                generation = data[2]
                print("Generation " + str(generation) + " Complete")
                #generate uuid
                ui_for_boid = uuid.uuid4()
                #save model
                torch.save(model.state_dict(), "boid_models/" + str(ui_for_boid) + ".pt")
                model_dict[ui_for_boid] = [boid_id,fitness,generation]

                #get new generation
                hof = get_new_generation(generation)
                save_best_of_gen(hof,generation,model_dict)
                choice_of_model = random.choice(hof)
                model = CNN()
                model.load_state_dict(torch.load("boid_models/" +str(choice_of_model) + ".pt"))
                mutate_model()
                bytes_to_send = struct.pack('%sf' % len(empty_message), *empty_message) #converting float to byte
                connection.sendall(bytes_to_send) #sending back
                connection.close()

            #-4 flag used to enter test mode
            elif array_received[0] == -4.0:
                print("Testing mode")
                model = CNN()
                # Get the best model from the generations
                model.load_state_dict(torch.load("boid_models/e958e1c8-86de-43d5-a5da-c85052d24d55.pt")) 
                #sending of data
                bytes_to_send = struct.pack('%sf' % len(empty_message), *empty_message) #converting float to byte
                connection.sendall(bytes_to_send) #sending back
                connection.close()

        #output error when problem with connecting
        except Exception as e:
            logging.error(traceback.format_exc())
            print("Error in connecting...")
            connection.sendall(bytearray([]))
            connection.close()
            break


def Convert_visual_array_to_tensor(array_recieved):
    #reshape array to rows of 16 with 2 values for each 'pixel' representing the different rays sent
    arr_3d = np.reshape(array_recieved, (16, 16, 2))
    # First convert to np.array then tensor to avoid non-writable tensor problem
    Tensor_of_visual = transforms.ToTensor()(np.array(arr_3d))
    #convert tensor to floats
    Tensor_of_visual = Tensor_of_visual.float()
    #add dimention to represent batchsize of size 1 so Conv2d can be used
    Tensor_of_visual = Tensor_of_visual.unsqueeze(0)
    return Tensor_of_visual


##creating CNN class
class CNN(nn.Module):
    def __init__(self):        
        super(CNN, self).__init__()

        #conv layers
        self.conv1 = nn.Conv2d(in_channels=2, out_channels = 4, kernel_size = 3, stride = 1, padding = 1)
        self.conv2 = nn.Conv2d(in_channels=4, out_channels=8, kernel_size=3, stride=1, padding=1)
        #relu maxpool and batchnorm
        self.Relu = nn.ReLU()
        self.maxpool = nn.MaxPool2d(kernel_size=2, stride=2)
        self.batchnorm1 = nn.BatchNorm2d(4)
        self.batchnorm2  =nn.BatchNorm2d(8)
        #linear layers
        self.fc1 = nn.Linear(in_features=128,out_features=32)
        self.fc2 = nn.Linear(in_features=32,out_features=12)
        self.fc3 = nn.Linear(in_features=12,out_features=3)

    def forward(self, x):
        # Input x has dimensions 1 x 2 x 16 x 16
        # Comments show tensor size output from previous line
        x = self.conv1(x)
        x = self.batchnorm1(x)
        x = self.Relu(x)
        x = self.maxpool(x)
        # 1 x 4 x 8 x 8

        #second conv layer
        x = self.conv2(x)
        x = self.batchnorm2(x)
        x = self.Relu(x)
        x = self.maxpool(x)
        # 1 x 8 x 4 x 4
        x = x.view(x.size(0), -1)
        # Flattened to 1 x 128
        x = self.fc1(x)
        # 1 x 32
        x = self.fc2(x)
        # 1 x 12
        x = self.fc3(x)
        # 1 x 3
        return x


#change weights of conv2d and linear layers
def weight_init(m):
    if isinstance(m, (nn.Conv2d,nn.Linear)):
        torch.nn.init.xavier_uniform_(m.weight.data)

def get_new_generation(generation):
    temp_dict = {}
    hof = []
    for keys in model_dict:
        if model_dict[keys][2] == generation:
            temp_dict[keys] = model_dict[keys][1] # adds just the fitnesses to a dictionary
    hof = nlargest(10, temp_dict, key = temp_dict.get) # gets list of the best 10 models by fitness
    return hof

## to print the generations hall of fame
def save_best_of_gen(hof,generation,model_dict):
    f = open("best_of_gen_", "a")
    f.write("\nGeneration: " + str(generation) + "\n")
    for key in hof:
        f.write(str(key) + " : " + str(model_dict[key][1]) + "\n")
    f.close()


    #mutate fully connected layer weights
def mutate_model():
    for weight in model.fc1.weight:
        for i in weight:
            if random.random() < mutprob:
                i = i+random.random() - (0.5)
    for weight in model.fc2.weight:
        for i in weight:
            if random.random() < mutprob:
                i = i+random.random() - (0.5)
    for weight in model.fc3.weight:
        for i in weight:
            if random.random() < mutprob:
                i = i+random.random() - (0.5)
    

# Instantiate the model - this initialises all weights and biases
model = CNN()


sending_and_reciveing(model) 
