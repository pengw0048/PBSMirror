%% ���ļ�
filename = 'D:\wifi\fastswitch.dat';
delimiter = '';
formatSpec = '%f%[^\n\r]';
fileID = fopen(filename,'r');
dataArray = textscan(fileID, formatSpec, 'Delimiter', delimiter, 'EmptyValue' ,NaN, 'ReturnOnError', false);
fclose(fileID);
dist = dataArray{:, 1};
clearvars filename delimiter formatSpec fileID dataArray ans;

%% ������ֱ��ͼ
figure
set(gcf,'position',[200,200,400,300])
hold on
box on
[N,edges]=histcounts(log(dist),80);
for i=1:size(N,2)
    fill([exp(edges(i)),exp(edges(i+1)),exp(edges(i+1)),exp(edges(i))],[1,1,N(i),N(i)],'w')
end
%set(gca,'yscale','log')
set(gca,'xscale','log')
ylim=get(gca,'ylim');
set(gca,'ylim',[1,ylim(2)])
set(gca,'xlim',[10^-2,10^6])
xlabel('��վ�л��ٶȣ���/�룩')
ylabel('����')
hold off
