%% 读文件
filename = 'D:\wifi\dist_bgm.dat';
delimiter = '';
formatSpec = '%f%[^\n\r]';
fileID = fopen(filename,'r');
dataArray = textscan(fileID, formatSpec, 'Delimiter', delimiter, 'EmptyValue' ,NaN, 'ReturnOnError', false);
fclose(fileID);
dist = dataArray{:, 1};
clearvars filename delimiter formatSpec fileID dataArray ans;


%% 画距离直方图
figure
set(gcf,'position',[200,200,800,300])
subplot(1,2,1)
hold on
box on
[N,edges]=histcounts(log(dist),80);
for i=1:size(N,2)
    fill([exp(edges(i)),exp(edges(i+1)),exp(edges(i+1)),exp(edges(i))],[1,1,N(i),N(i)],'b')
end
%set(gca,'yscale','log')
set(gca,'xscale','log')
ylim=get(gca,'ylim');
set(gca,'ylim',[1,ylim(2)])
set(gca,'xlim',[1,10^8])
xlabel('基站到WiFi接入点的距离（米）')
ylabel('数量')
hold off

subplot(1,2,2)
hold on
box on
xl=10.^(linspace(0,8,100));
yl=zeros(1,100);
for i=1:100
    yl(i)=size(dist(dist<xl(i)),1);
end
yl=yl/size(dist,1);
plot(xl,yl,'linewidth',2)
set(gca,'xscale','log')
ylim=get(gca,'ylim');
set(gca,'ylim',[0,1])
set(gca,'xlim',[1,10^8])
xlabel('基站到WiFi接入点的距离（米）')
ylabel('累计分布')
hold off
