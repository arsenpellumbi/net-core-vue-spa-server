FROM node:alpine AS client
EXPOSE 80
RUN mkdir /home/node/.npm-global
ENV PATH=/home/node/.npm-global/bin:$PATH
RUN npm install -g @quasar/cli
WORKDIR /app
CMD ["quasar", "dev", "--port", "80", "--host", "0.0.0.0"]
